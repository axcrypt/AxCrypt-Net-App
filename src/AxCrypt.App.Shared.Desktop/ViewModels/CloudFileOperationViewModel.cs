using AxCrypt.Abstractions;
using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Crypto.Asymmetric;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels
{
    public class CloudFileOperationViewModel : ViewModelBase
    {
        private FileStorageProvider _fileProviderService;
        private SecureFilesViewModel _securedFilesViewModel;
        private FileSystemState _fileSystemState = New<FileSystemState>();

        private ParallelFileOperation _fileOperation;
        public IEnumerable<UserPublicKey> Recipients { get; set; } = null;

        private string _decryptedPathForFileMove = null;

        public CloudFileOperationViewModel(
            FileStorageProvider fileProviderService,
            SecureFilesViewModel secureFilesViewModel
        )
        {
            _fileProviderService = fileProviderService;

            _securedFilesViewModel = secureFilesViewModel;
            _fileOperation = Resolve.ParallelFileOperation;
            Recipients = null;
        }

        #region Encryption

        public async Task Encrypt(IEnumerable<FilePickerItemViewModel> files)
        {
            if (files == null)
            {
                return;
            }

            await _fileOperation.DoCloudFilesAsync(
                files,
                ProcessFileEncryptionAsync,
                CheckEncryptionStatus
            );
        }

        private async Task<FileOperationContext> ProcessFileEncryptionAsync(
            FilePickerItemViewModel file,
            IProgressContext progress
        )
        {
            if (file.IsGoogleFileType)
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FileName,
                    ErrorStatus.FileFormatError
                );
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        Texts.IgnoreFileWarningText.InvariantFormat(file.FileName),
                        Common.DoNotShowAgainOptions.IgnoreFileWarning
                    );
                return fileOperationContext;
            }

            IDataStore originalFile = New<IDataStore>(file.FileID);
            if (file.Source != AxCrypt.Core.IO.FileProvider.Local)
            {
                using (
                    await New<IProgressDialog>()
                        .Show("Preparing file for encryption…", Texts.ProgressIndicatorWaitMessage)
                )
                {
                    string importFilePath = _fileProviderService.GetImportedFilePath(file.FileName);
                    IDataStore importFile = New<IDataStore>(importFilePath);
                    FileOperationContext preparingResult = await New<ImportedFileStorage>()
                        .CopyFileToImportedFiles(
                            async (fileStream) => await _fileProviderService.CopyFileToImportedFiles(file, fileStream),
                            importFile
                        );
                    if (preparingResult.ErrorStatus != ErrorStatus.Success)
                    {
                        New<IStatusChecker>()
                            .CheckStatusAndShowMessage(
                                preparingResult.ErrorStatus,
                                preparingResult.FullName,
                                preparingResult.InternalMessage
                            );
                        return preparingResult;
                    }

                    originalFile = New<IDataStore>(preparingResult.FullName);
                }
            }

            if (!await originalFile.IsEncryptableWithWarningAsync())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    originalFile.FullName,
                    ErrorStatus.FileAlreadyEncrypted
                );

                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );
                return fileOperationContext;
            }

            FileOperationContext context = await ProcessFileEncryption(originalFile, progress);
            if (context.ErrorStatus == ErrorStatus.Success)
            {
                await UpdateEncryptedFileStatusAsync(originalFile, file, file.Source);
                await New<FileSystemState>().Save();
            }

            return context;
        }

        private Task<FileOperationContext> ProcessFileEncryption(
            IDataStore actualFile,
            IProgressContext progressContext
        )
        {
            FileOperationsController operationsController = new FileOperationsController(
                progressContext
            );
            operationsController.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
            {
                using (FileLock lockedSave = e.SaveFileFullName.CreateUniqueFile())
                {
                    e.SaveFileFullName = lockedSave.DataStore.FullName;
                    lockedSave.DataStore.Delete();
                }
            };

            operationsController.QueryEncryptionPassphrase += (
                object sender,
                FileOperationEventArgs e
            ) =>
            { };

            operationsController.QuerySharedPublicKeys += (
                object sender,
                FileOperationEventArgs e
            ) =>
            { };

            operationsController.Completed += (object sender, FileOperationEventArgs e) =>
            {
                if (e.Status.ErrorStatus == ErrorStatus.FileAlreadyEncrypted)
                {
                    e.Status = new FileOperationContext(string.Empty, ErrorStatus.Success);
                    return;
                }

                if (e.Status.ErrorStatus != ErrorStatus.Success)
                {
                    return;
                }

                IDataStore encryptedInfo = New<IDataStore>(e.SaveFileFullName);
                IDataStore decryptedInfo = New<IDataStore>(FileOperation.GetTemporaryDestinationName(e.OpenFileFullName));

                ActiveFile activeFile = new ActiveFile(
                    encryptedInfo,
                    decryptedInfo,
                    e.LogOnIdentity,
                    ActiveFileStatus.NotDecrypted,
                    e.CryptoId
                );

                _fileSystemState.Add(activeFile);
            };

            return operationsController.EncryptFileAsync(actualFile, Recipients);
        }

        private static Task<bool> CheckEncryptionStatus(FileOperationContext foc)
        {
            if (foc.ErrorStatus == ErrorStatus.FileAlreadyEncrypted)
            {
                foc = new FileOperationContext(foc.FullName, ErrorStatus.Success);
            }

            return Task.FromResult(CheckStatusAndShowMessage(foc, string.Empty));
        }

        private static bool CheckStatusAndShowMessage(FileOperationContext context, string fallbackName)
        {
            return Resolve.StatusChecker.CheckStatusAndShowMessage(
                context.ErrorStatus,
                string.IsNullOrEmpty(context.FullName) ? fallbackName : context.FullName,
                context.InternalMessage
            );
        }

        private async Task UpdateEncryptedFileStatusAsync(IDataStore actualFile, FilePickerItemViewModel fileItem, AxCrypt.Core.IO.FileProvider fileSource)
        {
            ActiveFile encryptedFile = _fileSystemState.FindActiveFileFromEncryptedPath(
                MakeAxCryptFileName(actualFile.FullName)
            );

            if (encryptedFile == null)
            {
                return;
            }

            using (await New<IProgressDialog>().Show("Your file is being secured…", Texts.ProgressIndicatorWaitMessage))
            {
                if (fileSource != AxCrypt.Core.IO.FileProvider.Local &&
                    !await CheckEncryptedOriginalFileProcessed(actualFile, fileItem, encryptedFile))
                {
                    return;
                }

                if (fileSource == AxCrypt.Core.IO.FileProvider.Local &&
                     !await UploadEncryptedFileAsync(fileItem, encryptedFile))
                {
                    return;
                }
            }

            if (encryptedFile == null)
            {
                await _securedFilesViewModel.UpdateRecentFilesListAsync();
                return;
            }

            if (_securedFilesViewModel.CheckIfFileAlreadyInRecentFileList(encryptedFile))
            {
                return;
            }

            FileDetails newFile = new FileDetails(encryptedFile);
            _securedFilesViewModel.Files.Add(newFile);
            await _securedFilesViewModel.UpdateRecentFilesListAsync();
        }

        private async Task<bool> CheckEncryptedOriginalFileProcessed(
            IDataStore actualFile,
            FilePickerItemViewModel fileItem,
            ActiveFile encryptedFile
        )
        {
            string newFileId = await _fileProviderService.MoveFile(
                fileItem,
                encryptedFile.EncryptedFileInfo.Name,
                encryptedFile.EncryptedFileInfo
            );

            if (string.IsNullOrEmpty(newFileId))
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                        Common.DoNotShowAgainOptions.None
                    );
                return false;
            }

            if (
                !await _fileProviderService.DeleteFileAsync(
                    actualFile.FullName,
                    fileItem,
                    encryptedFile.EncryptedFileInfo.FullName,
                    newFileId
                )
            )
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when deleting the original file. The original left is left untouched and needs to be removed manually.",
                        Common.DoNotShowAgainOptions.None
                    );
                return false;
            }

            return true;
        }

        private async Task<bool> UploadEncryptedFileAsync(FilePickerItemViewModel fileItem, ActiveFile encryptedFile)
        {
            fileItem.FileID = fileItem.DestinationPath + encryptedFile.EncryptedFileInfo.Name;
            string newFileId = await _fileProviderService.MoveFile(
                            fileItem,
                            encryptedFile.EncryptedFileInfo.Name,
                            encryptedFile.EncryptedFileInfo
                        );

            if (string.IsNullOrEmpty(newFileId))
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                        Common.DoNotShowAgainOptions.None
                    );

                return false;
            }

            return true;
        }

        #endregion Encryption

        #region Decryption

        public async Task Decrypt(IEnumerable<FilePickerItemViewModel> files)
        {
            if (files == null)
            {
                return;
            }

            using (await New<IProgressDialog>().Show("Stop securing...", Texts.ProgressIndicatorWaitMessage))
            {
                await DecryptFiles(files);
            }
        }

        private async Task DecryptFiles(IEnumerable<FilePickerItemViewModel> files)
        {
            foreach (FilePickerItemViewModel fileItem in files)
            {
                if (fileItem.Source == AxCrypt.Core.IO.FileProvider.Local)
                {
                    await DecryptLocalFile(fileItem);
                    continue;
                }

                await DecryptCloudFile(fileItem);
            }

            await New<FileSystemState>().Save();
        }

        private async Task DecryptLocalFile(FilePickerItemViewModel fileItem)
        {
            IDataStore file = New<IDataStore>(fileItem.FileID);
            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FullName,
                    ErrorStatus.WrongFileExtensionError
                );
                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );
                return;
            }

            await InternalDecryptFile(file, fileItem, null, fileItem.Source);
        }

        private async Task DecryptCloudFile(FilePickerItemViewModel fileItem)
        {
            string fileName = fileItem.FileName;
            string fullFilePath = _fileProviderService.GetImportedFilePath(fileName);
            IDataStore file = New<IDataStore>(fullFilePath);
            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FullName,
                    ErrorStatus.WrongFileExtensionError
                );

                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );

                return;
            }

            FileOperationContext preparingResult = await New<ImportedFileStorage>()
                .CopyFileToImportedFiles(
                    async (fileStream) => await _fileProviderService.CopyFileToImportedFiles(fileItem, fileStream),
                    file
                );

            await DecryptPreparedFile(preparingResult, fileItem, fileItem.Source);
        }

        public async Task DecryptPreparedFile(
            FileOperationContext preparingResult,
            FilePickerItemViewModel fileItem,
            AxCrypt.Core.IO.FileProvider source
        )
        {
            if (preparingResult.ErrorStatus != ErrorStatus.Success)
            {
                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        preparingResult.ErrorStatus,
                        preparingResult.FullName,
                        preparingResult.InternalMessage
                    );
                return;
            }

            await InternalDecryptFile(New<IDataStore>(preparingResult.FullName), fileItem, null, source);
        }

        private async Task InternalDecryptFile(
            IDataStore actualFile,
            FilePickerItemViewModel fileItem,
            Passphrase passphrase,
            AxCrypt.Core.IO.FileProvider source
        )
        {
            FileOpenedContext operationContext = null;
            operationContext = await ProcessFileDecryption(actualFile, passphrase);
            if (operationContext.ErrorStatus == ErrorStatus.Canceled)
            {
                _securedFilesViewModel.AskFilePassword(
                    actualFile,
                    fileItem,
                    source,
                    new Microsoft.Maui.Controls.Command(SubmitFilePasswordForCloudFiles)
                );
                return;
            }
            if (operationContext.ErrorStatus != ErrorStatus.Success)
            {
                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        operationContext.ErrorStatus,
                        operationContext.FullName,
                        operationContext.InternalMessage
                    );

                return;
            }

            if (source != AxCrypt.Core.IO.FileProvider.Local)
            {
                await ProcessOriginalFileInCloudProviderForDecryption(fileItem);
            }

            if (operationContext.AddedFile == null)
            {
                await _securedFilesViewModel.UpdateRecentFilesListAsync();
                return;
            }

            if (!_securedFilesViewModel.CheckIfFileAlreadyInRecentFileList(operationContext.AddedFile))
            {
                return;
            }

            FileDetails newFile = new FileDetails(operationContext.AddedFile);
            await _securedFilesViewModel.RemoveFile(newFile);
            await _securedFilesViewModel.UpdateRecentFilesListAsync();
        }

        public async Task<FileOpenedContext> ProcessFileDecryption(
            IDataStore file,
            Passphrase passphrase
        )
        {
            // Avoid working with files in UI thread.
            return await Task.Run(async () =>
            {
                IExternalDataStore externalDataStore = file as IExternalDataStore;
                Task<IDisposable> openTask = Task.FromResult<IDisposable>(null);
                if (externalDataStore != null)
                {
                    // Loads external file before the long manipulations with it content.
                    // This allows to avoid multiple open/close operations.
                    openTask = externalDataStore.OpenAsync();
                }

                using (await openTask)
                {
                    ProgressContext progressContext = new ProgressContext();
                    FileOperationsController operationsController = new FileOperationsController(
                        progressContext
                    );

                    KnownIdentities knownIdentities = New<KnownIdentities>();
                    operationsController.QuerySaveFileAs += (
                        object sender,
                        FileOperationEventArgs e
                    ) =>
                    { };

                    operationsController.QueryDecryptionPassphrase = async (arg) =>
                    {
                        if (passphrase == null)
                        {
                            // If file password is unknown, cancel file decryption.
                            // When user input correct passphrase, DecryptAndLaunch can be called again.
                            arg.Cancel = true;
                            return;
                        }

                        LogOnIdentity identity = LogOnIdentity.Empty;
                        foreach (Passphrase candidate in _fileSystemState.KnownPassphrases)
                        {
                            if (candidate.Thumbprint == passphrase.Thumbprint)
                            {
                                identity = new LogOnIdentity(passphrase);
                                break;
                            }
                        }

                        if (identity == LogOnIdentity.Empty)
                        {
                            identity = new LogOnIdentity(passphrase);
                            _fileSystemState.KnownPassphrases.Add(passphrase);
                            await _fileSystemState.Save();
                        }

                        await knownIdentities.AddAsync(identity);
                        Resolve.UserSettings.EncryptionUpgradeMode =
                            EncryptionUpgradeMode.NotDecided;
                        arg.LogOnIdentity = identity;

                        return;
                    };

                    operationsController.KnownKeyAdded =
                        new AsyncDelegateAction<FileOperationEventArgs>(
                            async (FileOperationEventArgs e) =>
                            {
                                if (
                                    !_fileSystemState.KnownPassphrases.Any(i =>
                                        i.Thumbprint == e.LogOnIdentity.Passphrase.Thumbprint
                                    )
                                )
                                {
                                    _fileSystemState.KnownPassphrases.Add(
                                        e.LogOnIdentity.Passphrase
                                    );
                                }

                                await knownIdentities.AddAsync(e.LogOnIdentity);
                            }
                        );

                    operationsController.Completed += (object sender, FileOperationEventArgs e) =>
                    {
                        _decryptedPathForFileMove = e.SaveFileFullName;
                    };

                    FileOperationContext fileOperationContext =
                        await operationsController.DecryptFileAsync(file);
                    ActiveFile associatedFile = _fileSystemState.FindActiveFileFromEncryptedPath(
                        file.FullName
                    );
                    return new FileOpenedContext(fileOperationContext, associatedFile);
                }
            });
        }

        private async void SubmitFilePasswordForCloudFiles()
        {
            Passphrase? passphrase =
                await _securedFilesViewModel.FilePasswordViewModel.SubmitFilePassword();
            if (passphrase == null)
            {
                return;
            }

            await DecryptWithFilePassword(passphrase);
        }

        private async Task DecryptWithFilePassword(Passphrase passphrase)
        {
            using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorWaitMessage, Texts.ProgressIndicatorWaitMessage))
            {
                await InternalDecryptFile(
                    _securedFilesViewModel.FilePasswordViewModel.EncryptedFile,
                    _securedFilesViewModel.FileItemForFilePassword,
                    passphrase,
                    _securedFilesViewModel.FileSource
                );
            }
        }

        private async Task ProcessOriginalFileInCloudProviderForDecryption(FilePickerItemViewModel fileItem)
        {
            try
            {
                IDataStore file = New<IDataStore>(_decryptedPathForFileMove);
                if (file == null)
                {
                    return;
                }

                string newFileId = await _fileProviderService.MoveFile(fileItem, file.Name, file);
                if (string.IsNullOrEmpty(newFileId))
                {
                    await New<IPopup>()
                        .ShowAsync(
                            PopupButtons.Ok,
                            Texts.WarningTitle,
                            "Your file was successfully decrypted, however there was a problem when moving the decrypted original file. The encrypted left is left untouched and try again.",
                            Common.DoNotShowAgainOptions.None
                        );
                    return;
                }

                if (!await _fileProviderService.DeleteFileAsync(_decryptedPathForFileMove, fileItem, null))
                {
                    await New<IPopup>()
                        .ShowAsync(
                            PopupButtons.Ok,
                            Texts.WarningTitle,
                            "Your file was successfully decrypted, however there was a problem when deleting the original file. The encrypted left is left untouched and needs to be removed manually.",
                            Common.DoNotShowAgainOptions.None
                        );
                    return;
                }
            }
            catch (Exception ex)
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        ex.Message,
                        Common.DoNotShowAgainOptions.None
                    );
                return;
            }
        }

        #endregion Decryption

        #region ShareKey

        private IEnumerable<AxCrypt.Core.Crypto.Asymmetric.UserPublicKey>? _shareKeyUserList;

        private IList<string> _filesOrfolderPaths = new List<string>();

        public async Task<bool> ShareKey(
            IEnumerable<FilePickerItemViewModel> files,
            IEnumerable<AxCrypt.Core.Crypto.Asymmetric.UserPublicKey> userPublicKeys
        )
        {
            if (files == null)
            {
                return false;
            }

            if (userPublicKeys == null)
            {
                return false;
            }

            _shareKeyUserList = userPublicKeys;
            using (
                await New<IProgressDialog>()
                    .Show("Applying share key...", Texts.ProgressIndicatorWaitMessage)
            )
            {
                await ShareKeyFiles(files);
            }

            return true;
        }

        private async Task ShareKeyFiles(IEnumerable<FilePickerItemViewModel> files)
        {
            foreach (FilePickerItemViewModel fileItem in files)
            {
                if (fileItem.Source == AxCrypt.Core.IO.FileProvider.Local)
                {
                    await ShareKeyWithLocalFile(fileItem);
                    continue;
                }

                await ShareKeyWithCloudFile(fileItem);
            }

            await New<FileSystemState>().Save();
        }

        private async Task ShareKeyWithLocalFile(FilePickerItemViewModel localFileItem)
        {
            _filesOrfolderPaths.Add(localFileItem.FileID);
            IDataStore file = New<IDataStore>(localFileItem.FileID);
            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FullName,
                    ErrorStatus.WrongFileExtensionError
                );

                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );

                return;
            }

            await ProcessShareKey(file, localFileItem, New<KnownIdentities>().DefaultEncryptionIdentity, true);
        }

        private async Task ShareKeyWithCloudFile(FilePickerItemViewModel fileItem)
        {
            string fileName = fileItem.FileName;
            string fullFilePath = _fileProviderService.GetImportedFilePath(fileName);
            _filesOrfolderPaths.Add(fullFilePath);

            IDataStore file = New<IDataStore>(fullFilePath);
            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FullName,
                    ErrorStatus.WrongFileExtensionError
                );

                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );

                return;
            }

            FileOperationContext preparingResult = await New<ImportedFileStorage>()
                .CopyFileToImportedFiles(
                    async (fileStream) => await _fileProviderService.CopyFileToImportedFiles(fileItem, fileStream),
                    file
                );

            await ShareKeyPreparedFile(preparingResult, fileItem, false);
        }

        public async Task ShareKeyPreparedFile(FileOperationContext preparingResult, FilePickerItemViewModel fileItem, bool localFile)
        {
            if (preparingResult.ErrorStatus != ErrorStatus.Success)
            {
                New<IStatusChecker>()
                    .CheckStatusAndShowMessage(
                        preparingResult.ErrorStatus,
                        preparingResult.FullName,
                        preparingResult.InternalMessage
                    );
                return;
            }

            await ProcessShareKey(
                New<IDataStore>(preparingResult.FullName),
                fileItem,
                New<KnownIdentities>().DefaultEncryptionIdentity,
                localFile
            );
        }

        private async Task ProcessShareKey(IDataStore actualFile, FilePickerItemViewModel fileItem, LogOnIdentity identity, bool localFile)
        {
            if (!TryFindDecryptionKey(actualFile))
            {
                return;
            }

            await _filesOrfolderPaths.ChangeKeySharingAsync(_shareKeyUserList, identity);

            ActiveFile activeFile = await AddActiveFileToRecentFilesListAsync(actualFile);

            await UpdateOriginalFileAsync(actualFile, fileItem, activeFile, true);
        }

        private async Task<ActiveFile> AddActiveFileToRecentFilesListAsync(IDataStore actualFile)
        {
            IDataStore decryptedInfo = GetDecryptedInfo(actualFile);
            ActiveFile activeFile = new ActiveFile(
                actualFile,
                decryptedInfo,
                New<KnownIdentities>().DefaultEncryptionIdentity,
                ActiveFileStatus.NotDecrypted,
                Resolve.CryptoFactory.Default(New<ICryptoPolicy>()).CryptoId
            );

            _fileSystemState.Add(activeFile);
            await _fileSystemState.Save();

            FileOperationContext fileOperationContext = new FileOperationContext(
                actualFile.FullName,
                ErrorStatus.Success
            );

            FileOpenedContext fileOpenedContext = new FileOpenedContext(
                fileOperationContext,
                activeFile
            );

            if (fileOpenedContext.AddedFile == null)
            {
                await _securedFilesViewModel.UpdateRecentFilesListAsync();
                return null;
            }

            if (_securedFilesViewModel.CheckIfFileAlreadyInRecentFileList(fileOpenedContext.AddedFile))
            {
                FileDetails? existingFile = _securedFilesViewModel.Files.FirstOrDefault(f =>
                    f.FilePath == fileOpenedContext.AddedFile.EncryptedFileInfo.FullName
                );

                _securedFilesViewModel.Files.Remove(existingFile);
            }

            FileDetails sharedKeyFile = new FileDetails(activeFile);
            _securedFilesViewModel.Files.Add(sharedKeyFile);
            await _securedFilesViewModel.UpdateRecentFilesListAsync();

            return activeFile;
        }

        private static IDataStore GetDecryptedInfo(IDataStore actualFile)
        {
            EncryptedProperties properties = New<AxCryptFile>()
                .CreateEncryptedProperties(
                    actualFile,
                    New<KnownIdentities>().DefaultEncryptionIdentity
                );

            IDataStore decryptedInfo = New<IDataStore>(
                FileOperation.GetTemporaryDestinationName(
                    Resolve
                        .Portable.Path()
                        .Combine(
                            Resolve.Portable.Path().GetDirectoryName(actualFile.FullName),
                            properties.FileMetaData.FileName
                        )
                )
            );

            return decryptedInfo;
        }

        /**
                private async void SubmitFilePasswordForShareKey()
                {
                    Passphrase passphrase = await _securedFilesViewModel.FilePasswordViewModel.SubmitFilePassword();
                    if (passphrase == null)
                    {
                        return;
                    }
                    LogOnIdentity logOnIdentity = await AddPasswordToLogOnEntity(passphrase);
                    await ShareKeyWithFilePassword(_securedFilesViewModel.FilePasswordViewModel.EncryptedFile, _securedFilesViewModel.FileIdForFilePassword, logOnIdentity);
                }

                private async Task ShareKeyWithFilePassword(IDataStore actualFile, string fileId, LogOnIdentity identity)
                {
                    using (await New<IProgressDialog>().Show("Applying share key...", Texts.ProgressIndicatorWaitMessage))
                    {
                        await ShareKeyInternal(_securedFilesViewModel.FilePasswordViewModel.EncryptedFile, _securedFilesViewModel.FileIdForFilePassword, identity);
                    }
                }

                private async Task<LogOnIdentity> AddPasswordToLogOnEntity(Passphrase passphrase)
                {
                    if (passphrase == null)
                    {
                        return LogOnIdentity.Empty;
                    }

                    KnownIdentities knownIdentities = New<KnownIdentities>();
                    LogOnIdentity identity = LogOnIdentity.Empty;
                    foreach (Passphrase candidate in _fileSystemState.KnownPassphrases)
                    {
                        if (candidate.Thumbprint == passphrase.Thumbprint)
                        {
                            identity = new LogOnIdentity(passphrase);
                            break;
                        }
                    }

                    if (identity == LogOnIdentity.Empty)
                    {
                        identity = new LogOnIdentity(passphrase);
                    }

                    if (!_fileSystemState.KnownPassphrases.Any(i => i.Thumbprint == identity.Passphrase.Thumbprint))
                    {
                        _fileSystemState.KnownPassphrases.Add(identity.Passphrase);
                        await _fileSystemState.Save();
                    }

                    await knownIdentities.AddAsync(identity);
                    Resolve.UserSettings.EncryptionUpgradeMode = EncryptionUpgradeMode.NotDecided;

                    return identity;
                }

                **/

        private static bool TryFindDecryptionKey(IDataStore fileInfo)
        {
            Guid cryptoId;
            LogOnIdentity logOnIdentity = fileInfo.TryFindPassphrase(out cryptoId);
            if (logOnIdentity == null)
            {
                return false;
            }

            return true;
        }

        private async Task<bool> UpdateOriginalFileAsync(
            IDataStore actualFile,
            FilePickerItemViewModel fileItem,
            ActiveFile encryptedFile,
            bool renameOnDelete = false
        )
        {
            string newFileId = await _fileProviderService.MoveFile(
                fileItem,
                encryptedFile.EncryptedFileInfo.Name,
                encryptedFile.EncryptedFileInfo
            );

            if (string.IsNullOrEmpty(newFileId))
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                        Common.DoNotShowAgainOptions.None
                    );

                return false;
            }

            if (
                !await _fileProviderService.DeleteFileAsync(
                    string.Empty,
                    fileItem,
                    encryptedFile.EncryptedFileInfo.FullName,
                    newFileId,
                    renameOnDelete
                )
            )
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when deleting the original file. The original left is left untouched and needs to be removed manually.",
                        Common.DoNotShowAgainOptions.None
                    );
                return false;
            }

            return true;
        }

        #endregion ShareKey

        #region UtiltyMethods

        public string GetImportedFilePath(string fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (_fileProviderService == null)
            {
                return string.Empty;
            }

            return _fileProviderService.GetImportedFilePath(fileName);
        }

        private string MakeAxCryptFileName(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("fileName");
            }

            string axCryptExtension = OS.Current.AxCryptExtension;
            string originalExtension = Resolve.Portable.Path().GetExtension(fileName);
            string modifiedExtension =
                originalExtension.Length == 0 ? String.Empty : "-" + originalExtension.Substring(1);
            string modifiedFileName = fileName;
            if (originalExtension != string.Empty)
            {
                modifiedFileName = fileName.Replace(originalExtension, modifiedExtension);
            }
            string axCryptFileName = modifiedFileName + axCryptExtension;

            return axCryptFileName;
        }

        #endregion UtiltyMethods
    }
}