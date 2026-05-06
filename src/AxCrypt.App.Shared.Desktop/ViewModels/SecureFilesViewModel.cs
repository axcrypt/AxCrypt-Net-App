using AxCrypt.Abstractions;
using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.Desktop.UI.Services;
using AxCrypt.App.Shared.Desktop.ViewModels.RecentFiles;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels
{
    public class SecureFilesViewModel : ViewModelBase
    {
        private RecentFilesProvider _recentFilesProvider = new RecentFilesProvider();
        private ICustomNavigationService? _navigationManager;

        public SecureFilesViewModel()
        {
            OpenFileCommand = new Command<FileDetails>(OpenFile);
            AddFileCommand = new Command<object>(AddFile);
            Files = new List<FileDetails>();
            SelectedFiles = new List<FileDetails>();
            StartUpdatingRecentFiles();
        }

        public SecureFilesViewModel(bool hasEncryptionCapability, IDictionary<string, object> selectedFileData,
            ICustomNavigationService navigationManager)
        : this()
        {
            _navigationManager = navigationManager;

            if (selectedFileData.Count > 0
                && selectedFileData.ContainsKey(nameof(FilePickerItemViewModel)))
            {
                FileItems =
                    (IEnumerable<FilePickerItemViewModel>)
                        selectedFileData[nameof(FilePickerItemViewModel)];

                SelectedFileStorageProvider = (FileStorageProvider)
                    selectedFileData[nameof(FileStorageProvider)];
            }
        }

        public async Task TriggerFileOperationProcess()
        {
            await ProcessFileOperationAsync();
        }

        public IList<FileDetails> Files
        {
            get { return GetProperty<IList<FileDetails>>(nameof(Files)); }
            private set { SetProperty(nameof(Files), value); }
        }

        public ICommand OpenFileCommand { get; private set; }

        public ICommand AddFileCommand { get; private set; }

        public IList<FileDetails> SelectedFiles
        {
            get { return GetProperty<IList<FileDetails>>(nameof(SelectedFiles)); }
            private set { SetProperty(nameof(SelectedFiles), value); }
        }

        public FileDetails SelectedFileItemInfo
        {
            get { return GetProperty<FileDetails>(nameof(SelectedFileItemInfo)); }
            private set { SetProperty(nameof(SelectedFileItemInfo), value); }
        }

        public DesktopFilePasswordViewModel FilePasswordViewModel
        {
            get { return GetProperty<DesktopFilePasswordViewModel>(nameof(FilePasswordViewModel)); }
            private set
            {
                SetProperty<DesktopFilePasswordViewModel>(nameof(FilePasswordViewModel), value);
            }
        }

        public IEnumerable<FilePickerItemViewModel> FileItems
        {
            get { return GetProperty<IEnumerable<FilePickerItemViewModel>>(nameof(FileItems)); }
            set
            {
                base.SetProperty<IEnumerable<FilePickerItemViewModel>>(nameof(FileItems), value);
            }
        }

        public FileStorageProvider SelectedFileStorageProvider
        {
            get
            {
                return base.GetProperty<FileStorageProvider>(nameof(SelectedFileStorageProvider));
            }
            set
            {
                base.SetProperty<FileStorageProvider>(nameof(SelectedFileStorageProvider), value);
            }
        }

        private async void OpenFile(FileDetails file)
        {
            if (SelectedFiles.Count > 0)
            {
                AddSelectedFileToList(file);
                return;
            }

            await DecryptFile(New<IDataStore>(file.FilePath), null!);
        }

        private async void AddFile(object anchorView)
        {
            IFilePicker filePicker = New<IFilePicker>();
            FilePickerParameters filePickerParameters = new FilePickerParameters
            {
                Filter = FilePickerFilter.AxCryptFiles,
                DisplayngAnchorView = anchorView,
            };

            IDataStore file = await filePicker.ChooseFileAsync(filePickerParameters);
            if (file == null)
            {
                return;
            }

            FileOperationContext preparingResult = await New<ImportedFileStorage>()
                .CopyFileToImportedFiles(file);

            await DecryptPreparedFile(preparingResult);
        }

        public async Task DecryptPreparedFile(FileOperationContext preparingResult)
        {
            if (preparingResult.ErrorStatus != ErrorStatus.Success)
            {
                New<IStatusChecker>().CheckStatusAndShowMessage(preparingResult.ErrorStatus, preparingResult.FullName,
                    preparingResult.InternalMessage);

                return;
            }

            await DecryptFile(New<IDataStore>(preparingResult.FullName), null!);
        }

        private async Task DecryptFile(IDataStore file, Passphrase passphrase)
        {
            FileOpenedContext operationContext = null!;
            if (New<UserSettings>().ShouldNotifyUserAboutCleaningWorkflow)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.CleanupWorkflowDescription);
                New<UserSettings>().ShouldNotifyUserAboutCleaningWorkflow = false;
            }

            using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorDecryptingMessage, Texts.ProgressIndicatorWaitMessage))
            {
                operationContext = await new CloudFileOperationViewModel(SelectedFileStorageProvider, this).DecryptAndLaunch(file, passphrase);
            }

            if (operationContext.ErrorStatus == ErrorStatus.Success)
            {
                if (operationContext.AddedFile == null)
                {
                    await UpdateRecentFilesListAsync();
                    return;
                }

                if (CheckIfFileAlreadyInRecentFileList(operationContext.AddedFile))
                {
                    return;
                }

                FileDetails newFile = new FileDetails(operationContext.AddedFile);
                Files.Add(newFile);
                return;
            }

            if (operationContext.ErrorStatus == ErrorStatus.Canceled)
            {
                AskFilePassword(file);
                return;
            }

            New<IStatusChecker>().CheckStatusAndShowMessage(operationContext.ErrorStatus, operationContext.FullName,
                operationContext.InternalMessage);
        }

        public bool CheckIfFileAlreadyInRecentFileList(ActiveFile addedFile)
        {
            bool isAlreadyInRecent = Files.Any(f => f.FilePath == addedFile.EncryptedFileInfo.FullName);

            if (!isAlreadyInRecent)
            {
                return false;
            }

            if (isAlreadyInRecent && !addedFile.IsShared)
            {
                return true;
            }

            FileDetails? existingFile = Files.FirstOrDefault(f => f.FilePath == addedFile.EncryptedFileInfo.FullName);

            if (existingFile!.SharedWith.Any())
            {
                return true;
            }

            Files[Files.IndexOf(existingFile)] = new FileDetails(addedFile);
            return true;
        }

        private void AskFilePassword(IDataStore dataStore)
        {
            FilePasswordViewModel = new DesktopFilePasswordViewModel(dataStore, new Command(SubmitFilePassword));
        }

        private async void SubmitFilePassword()
        {
            try
            {
                Passphrase? passphrase = await FilePasswordViewModel.SubmitFilePassword();
                if (passphrase == null)
                {
                    return;
                }

                await DecryptFile(FilePasswordViewModel.EncryptedFile, passphrase);
            }
            catch (Exception)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, "Something went wrong!");
            }
        }

        private async void StartUpdatingRecentFiles()
        {
            await UpdateRecentFilesListAsync();
        }

        public async Task UpdateRecentFilesListAsync()
        {
            IEnumerable<ActiveFile> activeFiles = await _recentFilesProvider.LoadRecentFiles();
            Files = new List<FileDetails>(activeFiles.Select(a => new FileDetails(a)));
        }

        private void AddSelectedFileToList(FileDetails file)
        {
            if (SelectedFiles.Contains(file))
            {
                SelectedFiles.Remove(file);
            }

            SelectedFiles.Add(file);
        }

        public async Task RemoveFile(FileDetails file)
        {
            try
            {
                bool isRemovedSuccessfully = await _recentFilesProvider.RemoveFilesFromRecent(
                    new List<IDataStore>() { New<IDataStore>(file.FilePath) }
                );

                if (isRemovedSuccessfully)
                {
                    Files.Remove(file);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return;
            }
        }

        public FilePickerItemViewModel FileItemForFilePassword
        {
            get { return GetProperty<FilePickerItemViewModel>(nameof(FileItemForFilePassword)); }
            private set
            {
                SetProperty<FilePickerItemViewModel>(nameof(FileItemForFilePassword), value);
            }
        }

        public AxCrypt.Core.IO.FileProvider FileSource
        {
            get { return GetProperty<AxCrypt.Core.IO.FileProvider>(nameof(FileSource)); }
            private set { SetProperty(nameof(FileSource), value); }
        }

        public void AskFilePassword(IDataStore dataStore, FilePickerItemViewModel fileItem, AxCrypt.Core.IO.FileProvider fileSource, System.Windows.Input.ICommand submitPasswordCommand)
        {
            try
            {
                FilePasswordViewModel = new DesktopFilePasswordViewModel(dataStore, submitPasswordCommand);
                FileItemForFilePassword = fileItem;
                FileSource = fileSource;
            }
            catch (Exception ex)
            {
                Task.Run(async () => { await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message); });
            }
        }

        private async Task ProcessFileOperationAsync()
        {
            if (SelectedFileStorageProvider == null)
            {
                return;
            }

            switch (SelectedFileStorageProvider.SelectedFileOperation)
            {
                case FileOperationOption.OpenSecured:
                    await OpenSecuredFile();
                    break;

                case FileOperationOption.Encrypt:
                    await EncryptSelectedFile();
                    break;

                case FileOperationOption.Decrypt:
                    await DecryptSelectedFile();
                    break;

                case FileOperationOption.ShareKey:
                    await ShareKeysOnSelectedFiles();
                    break;

                default:
                    break;
            }
        }

        private async Task OpenSecuredFile()
        {
            FilePickerItemViewModel selectedFile = FileItems.FirstOrDefault()!;
            if (selectedFile == null)
            {
                return;
            }

            string? fileName = selectedFile.FileName;
            string fullFilePath = SelectedFileStorageProvider.GetImportedFilePath(fileName!);
            IDataStore file = New<IDataStore>(fullFilePath);

            if (!file.IsEncrypted())
            {
                FileOperationContext fileOperationContext = new FileOperationContext(file.FullName,
                    ErrorStatus.WrongFileExtensionError);

                New<IStatusChecker>().CheckStatusAndShowMessage(fileOperationContext.ErrorStatus, fileOperationContext.FullName,
                        fileOperationContext.InternalMessage);

                return;
            }

            FileOperationContext preparingResult = await New<ImportedFileStorage>().CopyFileToImportedFiles(
                async (fileStream) => await SelectedFileStorageProvider.CopyFileToImportedFiles(selectedFile, fileStream),
                file
            );

            await DecryptPreparedFile(preparingResult);
        }

        public async Task EncryptSelectedFile()
        {
            if (FileItems == null)
            {
                throw new IncorrectDataException();
            }

            await new CloudFileOperationViewModel(SelectedFileStorageProvider, this).Encrypt(FileItems);
        }

        private async Task DecryptSelectedFile()
        {
            if (FileItems == null)
            {
                throw new IncorrectDataException();
            }

            await new CloudFileOperationViewModel(SelectedFileStorageProvider, this).Decrypt(FileItems);
        }

        private ShareKeyViewModel? _shareKeyViewModel;

        public async Task ShareKeysOnSelectedFiles()
        {
            if (FileItems == null)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.ExactlySelectOneFileText);
                return;
            }

            CloudFileOperationViewModel fileOperationViewModel = new CloudFileOperationViewModel(
                SelectedFileStorageProvider, this);

            IList<FileOperationContext> ShareKeyFileListItem = new List<FileOperationContext>();

            foreach (FilePickerItemViewModel fileItem in FileItems)
            {
                string fullFilePath = GetFullPathByFileSource(fileItem, fileOperationViewModel);
                IDataStore file = New<IDataStore>(fullFilePath);

                if (!file.IsEncrypted())
                {
                    FileOperationContext fileOperationContext = new FileOperationContext(file.FullName,
                        ErrorStatus.WrongFileExtensionError);

                    New<IStatusChecker>().CheckStatusAndShowMessage(
                        fileOperationContext.ErrorStatus,
                        fileOperationContext.FullName,
                        fileOperationContext.InternalMessage
                    );

                    return;
                }

                if (fullFilePath == string.Empty)
                {
                    continue;
                }

                ShareKeyFileListItem.Add(await fileOperationViewModel.ShareKeyWithCloudFile(fileItem));
            }

            IEnumerable<string> ShareKeyFilepathList = ShareKeyFileListItem.Select(f => f.FullName);

            SharingListViewModel sharingListViewModel = await SharingListViewModel.CreateForFilesAsync(
                                                             ShareKeyFilepathList,
                                                             Resolve.KnownIdentities.DefaultEncryptionIdentity
                                                        );

            _shareKeyViewModel = AxCServiceProvider.GetService<ShareKeyViewModel>();

            await _shareKeyViewModel!.SetSelectedFilesOrFolders(FileItems.Select(e => e.FileName), sharingListViewModel, true);

            if (_shareKeyViewModel.PageResult == DialogResult.Cancel)
            {
                foreach (string itemPath in ShareKeyFileListItem.Select(f => f.FullName))
                {
                    IDataStore file = New<IDataStore>(itemPath);
                    file.Delete();
                }

                return;
            }

            await fileOperationViewModel.ShareKeyPreparedFile(ShareKeyFileListItem, FileItems, sharingListViewModel!.SharedWith);
        }

        private string GetFullPathByFileSource(FilePickerItemViewModel selectedFileItem,
            CloudFileOperationViewModel fileOperationViewModel
        )
        {
            if (selectedFileItem == null)
            {
                return string.Empty;
            }

            if (selectedFileItem.Source == AxCrypt.Core.IO.FileProvider.Local)
            {
                return selectedFileItem.FileID!;
            }

            return fileOperationViewModel.GetImportedFilePath(selectedFileItem.FileName!);
        }
    }
}