using AxCrypt.Abstractions;
using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.UI.ViewModels;
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
        private IdentityViewModel _identityViewModel { get; set; }

        private ParallelFileOperation _fileOperation;
        public IEnumerable<UserPublicKey> Recipients { get; set; } = null;

        private string _decryptedPathForFileMove = null;

        public CloudFileOperationViewModel(
            FileStorageProvider fileProviderService,
            SecureFilesViewModel secureFilesViewModel
        )
        {
            _identityViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel.IdentityViewModel;
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

            FileOperationContext context = await ProcessFileEncryption(originalFile, progress, file);

            return context;
        }

        private Task<FileOperationContext> ProcessFileEncryption(IDataStore actualFile, IProgressContext progressContext,
            FilePickerItemViewModel file)
        {
            FileOperationsController operationsController = new FileOperationsController(progressContext);

            operationsController.QuerySaveFileAs += (object sender, FileOperationEventArgs e) =>
            {
                using (FileLock lockedSave = e.SaveFileFullName.CreateUniqueFile())
                {
                    e.SaveFileFullName = lockedSave.DataStore.FullName;
                    lockedSave.DataStore.Delete();
                }
            };

            operationsController.QueryEncryptionPassphrase += (object sender, FileOperationEventArgs e) => { };
            operationsController.QuerySharedPublicKeys += (object sender, FileOperationEventArgs e) => { };

            operationsController.Completed += async (object sender, FileOperationEventArgs e) =>
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

                ActiveFile activeFile = new ActiveFile(encryptedInfo, decryptedInfo, e.LogOnIdentity,
                    ActiveFileStatus.NotDecrypted, e.CryptoId);

                await UpdateEncryptedFileStatusAsync(actualFile, activeFile, file, file.Source);
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
            return Resolve.StatusChecker.CheckStatusAndShowMessage(context.ErrorStatus,
                string.IsNullOrEmpty(context.FullName) ? fallbackName : context.FullName,
                context.InternalMessage
            );
        }

        private async Task UpdateEncryptedFileStatusAsync(IDataStore actualFile, ActiveFile encryptedFile, FilePickerItemViewModel fileItem, AxCrypt.Core.IO.FileProvider fileSource)
        {
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
        }

        private async Task<bool> CheckEncryptedOriginalFileProcessed(IDataStore actualFile, FilePickerItemViewModel fileItem,
            ActiveFile encryptedFile)
        {
            string newFileId = await _fileProviderService.MoveFile(fileItem, encryptedFile.EncryptedFileInfo.Name,
                encryptedFile.EncryptedFileInfo
            );

            if (string.IsNullOrEmpty(newFileId))
            {
                await New<IPopup>().ShowAsync(
                    PopupButtons.Ok,
                    Texts.WarningTitle,
                    "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                    Common.DoNotShowAgainOptions.None
                );

                return false;
            }

            if (!await _fileProviderService.DeleteFileAsync(actualFile.FullName, fileItem, encryptedFile.EncryptedFileInfo.FullName))
            {
                await New<IPopup>().ShowAsync(
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
            fileItem.FileID = fileItem.ParentPath + "/" + encryptedFile.EncryptedFileInfo.Name;

            string newFileId = await _fileProviderService.MoveFile(fileItem, encryptedFile.EncryptedFileInfo.Name,
                            encryptedFile.EncryptedFileInfo);

            if (string.IsNullOrEmpty(newFileId))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle,
                        "Your file was successfully encrypted, however there was a problem when moving the encrypted file. The encrypted left is not updated and try again.",
                        Common.DoNotShowAgainOptions.None);

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

        private async Task InternalDecryptFile(IDataStore actualFile, FilePickerItemViewModel fileItem,
            Passphrase passphrase, FileProvider source)
        {
            FileOpenedContext operationContext = null;
            operationContext = await ProcessFileDecryption(actualFile, passphrase);
            if (operationContext.ErrorStatus == ErrorStatus.Canceled)
            {
                return;
            }

            if (operationContext.ErrorStatus != ErrorStatus.Success)
            {
                New<IStatusChecker>().CheckStatusAndShowMessage(operationContext.ErrorStatus, operationContext.FullName,
                    operationContext.InternalMessage);

                return;
            }

            if (source != AxCrypt.Core.IO.FileProvider.Local)
            {
                await ProcessOriginalFileInCloudProviderForDecryption(fileItem);
            }
        }

        public async Task<FileOpenedContext> ProcessFileDecryption(IDataStore file, Passphrase passphrase)
        {
            FileOperationsController operationsController = InitalizeFileOperation();

            // Avoid working with files in UI thread.
            return await Task.Run(async () =>
            {
                FileOperationContext fileOperationContext = await operationsController.DecryptFileAsync(file);
                ActiveFile associatedFile = _fileSystemState.FindActiveFileFromEncryptedPath(file.FullName);
                return new FileOpenedContext(fileOperationContext, associatedFile);
            });
        }

        public async Task<FileOpenedContext> DecryptAndLaunch(IDataStore file, Passphrase passphrase)
        {
            FileOperationsController operationsController = InitalizeFileOperation();

            // Avoid working with files in UI thread.
            return await Task.Run(async () =>
            {
                FileOperationContext fileOperationContext = await operationsController.DecryptAndLaunchAsync(file);
                ActiveFile associatedFile = _fileSystemState.FindActiveFileFromEncryptedPath(file.FullName);
                return new FileOpenedContext(fileOperationContext, associatedFile);
            });
        }

        private FileOperationsController InitalizeFileOperation()
        {
            ProgressContext progressContext = new ProgressContext();
            FileOperationsController operationsController = new FileOperationsController(progressContext);

            KnownIdentities knownIdentities = New<KnownIdentities>();

            operationsController.QueryDecryptionPassphrase = QueryDecryptPassphraseAsync;
            operationsController.QuerySaveFileAs += (object sender, FileOperationEventArgs e) => { };

            operationsController.KnownKeyAdded = new AsyncDelegateAction<FileOperationEventArgs>(async (FileOperationEventArgs e) =>
            {
                if (!_fileSystemState.KnownPassphrases.Any(i => i.Thumbprint == e.LogOnIdentity.Passphrase.Thumbprint))
                {
                    _fileSystemState.KnownPassphrases.Add(e.LogOnIdentity.Passphrase);
                }

                await knownIdentities.AddAsync(e.LogOnIdentity);
            });

            operationsController.Completed += (object sender, FileOperationEventArgs e) =>
            {
                _decryptedPathForFileMove = e.SaveFileFullName;
                return Task.CompletedTask;
            };

            return operationsController;
        }

        private async Task QueryDecryptPassphraseAsync(FileOperationEventArgs e)
        {
            await _identityViewModel.AskForDecryptPassphrase.ExecuteAsync(e.OpenFileFullName);
            if (_identityViewModel.LogOnIdentity == LogOnIdentity.Empty)
            {
                e.Cancel = true;
                return;
            }
            e.LogOnIdentity = _identityViewModel.LogOnIdentity;
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

        public async Task<FileOperationContext> ShareKeyWithCloudFile(FilePickerItemViewModel fileItem)
        {
            string fullFilePath = _fileProviderService.GetImportedFilePath(fileItem.FileName);

            IDataStore file = New<IDataStore>(fullFilePath);
            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(
                    file.FullName,
                    ErrorStatus.WrongFileExtensionError
                );

                New<IStatusChecker>().CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );

                return fileOperationContext;
            }

            return await New<ImportedFileStorage>().CopyFileToImportedFiles(
                    async (fileStream) => await _fileProviderService.CopyFileToImportedFiles(fileItem, fileStream),
                    file
            );
        }

        public async Task ShareKeyPreparedFile(IEnumerable<FileOperationContext> shareKeyFileList, IEnumerable<FilePickerItemViewModel> fileItem, IEnumerable<UserPublicKey> userPublicKeys)
        {
            if (userPublicKeys == null)
            {
                return;
            }

            await shareKeyFileList.Select(f => f.FullName).ChangeKeySharingAsync(userPublicKeys, New<KnownIdentities>().DefaultEncryptionIdentity);

            IList<FilePickerItemViewModel> itemList = fileItem.ToList();
            int i = 0;

            ShareRequest request = new ShareRequest();
            request.RecipientEmailList = userPublicKeys.Select(e => e.Email.ToString()).ToList();

            foreach (FileOperationContext sharedFileInfo in shareKeyFileList)
            {
                if (sharedFileInfo.ErrorStatus != ErrorStatus.Success)
                {
                    New<IStatusChecker>().CheckStatusAndShowMessage(sharedFileInfo.ErrorStatus, sharedFileInfo.FullName,
                            sharedFileInfo.InternalMessage);

                    return;
                }

                await ProcessShareKey(New<IDataStore>(sharedFileInfo.FullName), itemList[i], request);
                i++;
            }
        }

        private async Task ProcessShareKey(IDataStore fileInfo, FilePickerItemViewModel cloudFileItem, ShareRequest request)
        {
            if (!TryFindDecryptionKey(fileInfo))
            {
                return;
            }

            await UpdateShareKeyFileAsync(fileInfo, cloudFileItem, true);
            if (request.RecipientEmailList.Any())
            {
                await _fileProviderService.ShareFileAsync(cloudFileItem.FileID, request);
            }
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

        private async Task<bool> UpdateShareKeyFileAsync(IDataStore fileInfo, FilePickerItemViewModel cloudFileItem,
            bool renameOnDelete = false)
        {
            return await _fileProviderService.UpdateFile(cloudFileItem, fileInfo);
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