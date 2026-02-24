using AxCrypt.App.Shared.CloudCore;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels.FileBrowser
{
    public class FilePickerViewModel(
        ShareKeyViewModel shareKeyViewModel,
        FileProviderSelectionViewModel fileProviderSelectionViewModel,
        ICustomNavigationService navigationManager
    ) : ViewModelBase
    {
        protected bool _isVisible = false;

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                bool changed = _isVisible != value;
                _isVisible = value;
                if (changed)
                {
                    UpdateViewState();
                }
            }
        }

        private string ROOT_DIR = "";

        protected FileStorageProvider _fileProviderService;
        protected static bool _hasPaidSubscription;

        protected ICustomNavigationService _navigationManager = navigationManager;

        public bool ShowEmptyFolderView
        {
            get { return GetProperty<bool>(nameof(ShowEmptyFolderView)); }
            private set { SetProperty<bool>(nameof(ShowEmptyFolderView), value); }
        }

        public IList<FilePickerItemViewModel> SelectedFiles
        {
            get { return GetProperty<List<FilePickerItemViewModel>>(nameof(SelectedFiles)); }
            private set { SetProperty(nameof(SelectedFiles), value); }
        }

        public ObservableCollection<FilePickerItemViewModel> Files
        {
            get
            {
                return GetProperty<ObservableCollection<FilePickerItemViewModel>>(nameof(Files));
            }
            private set
            {
                SetProperty(nameof(Files), value);
                ShowHideEmptyFolderView();
            }
        }

        public string PageTitle { get; set; } = "";

        public string SelectedFilePath { get; set; } = "";

        public string SelectedCloudProvider { get; set; } = "";

        public string CurrentFolder
        {
            get { return GetProperty<string>(nameof(CurrentFolder)); }
            set { SetProperty(nameof(CurrentFolder), value); }
        }

        public bool MultiSelectEnabled
        {
            get { return GetProperty<bool>(nameof(MultiSelectEnabled)); }
            set { SetProperty(nameof(MultiSelectEnabled), value); }
        }

        private void ShowHideEmptyFolderView()
        {
            if (Files.Count > 0)
            {
                ShowEmptyFolderView = false;
                return;
            }

            ShowEmptyFolderView = true;
        }

        public string? CurrentFolderID { set; get; }

        public List<FolderItem> OpenedFoldersList = new List<FolderItem>();

        public Dictionary<string, object>? SelectedFileData { get; set; }

        public FileOperationOption SelectedFileOperation { get; set; }

        public LogOnViewModel? LogOnViewModel { get; set; } = AxCServiceProviderExtension.LogOnViewModel;

        private MainViewModel _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;

        private FileOperationViewModel _fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel!.FileOperationViewModel;

        private ShareKeyViewModel? _sharekeyViewModel = shareKeyViewModel;

        private FileProviderSelectionViewModel _fileProviderSelectionViewModel = fileProviderSelectionViewModel;

        private UpgradeSubscriptionViewModel _upgradeViewModel = AxCServiceProviderExtension.UpgradeSubscriptionViewModel!;

        public void InitializeFilePickerDialog(
            FileStorageProvider fileProviderService,
            bool hasPaidSubscription
        )
        {
            _hasPaidSubscription = hasPaidSubscription;
            _fileProviderService = fileProviderService;

            ROOT_DIR = _fileProviderService.PageTitle;
            PageTitle = _fileProviderService.PageTitle;
            CurrentFolder = _fileProviderService.PageTitle;
            SelectedFileOperation = _fileProviderService.SelectedFileOperation;
            Files = new ObservableCollection<FilePickerItemViewModel>(_fileProviderService.Files);
            SelectedCloudProvider = _fileProviderSelectionViewModel.SelectedFileProvider?.Image ?? "";

            MultiSelectEnabled = false;
            SelectedFiles = new List<FilePickerItemViewModel>();
            OpenedFoldersList.Clear();
            IsVisible = true;
        }

        public bool IsFileSelected(FilePickerItemViewModel file)
        {
            return SelectedFiles.Contains(file);
        }

        public async Task ProcessSelectedFile(FilePickerItemViewModel selectedFile)
        {
            if (selectedFile == null)
            {
                throw new InvalidOperationException($"{nameof(selectedFile)} should not be null!");
            }

            if (selectedFile.IsFolder)
            {
                await LoadSelectedFolderItems(selectedFile);
                return;
            }

            if (MultiSelectEnabled)
            {
                UpdateSelectedFileList(selectedFile);
                return;
            }

            UpdateSelectedFileList(selectedFile);
            await ProcessSelectedFileItemActionAsync();
        }

        private async Task LoadSelectedFolderItems(FilePickerItemViewModel fileItem)
        {
            if (!OpenedFoldersList.Any(fi => fi.FileID == fileItem.FileID))
            {
                AddOpenedFoldersList(
                    fileItem.FileID,
                    fileItem.FileName,
                    CurrentFolder,
                    CurrentFolderID
                );
                MultiSelectEnabled = false;
            }

            ChangeFolder(fileItem.FileName, fileItem.FileID);
            await LoadFolderAsync(fileItem.FileID, fileItem.FileName);
        }

        private void AddOpenedFoldersList(
            string fileID,
            string currentDirectory,
            string parent,
            string parentID
        )
        {
            if (parent == null && parentID == null)
            {
                parent = ROOT_DIR;
            }

            OpenedFoldersList.Add(
                new FolderItem()
                {
                    FileID = fileID,
                    DirectoryName = currentDirectory,
                    ParentDirectory = parent,
                    ParentDirectoryID = parentID
                }
            );
        }

        private void ChangeFolder(string Name, string fileID)
        {
            CurrentFolder = Name;
            CurrentFolderID = fileID;
        }

        private async Task LoadFolderAsync(string fileId, string fileName)
        {
            if (fileId == null && fileName == null)
            {
                throw new ArgumentNullException(nameof(fileId));
            }

            await _fileProviderService.ListFilesAsync(fileId);
            Files = new ObservableCollection<FilePickerItemViewModel>(_fileProviderService.Files);
        }

        public void ProcessMultiSelectedFile(FilePickerItemViewModel selectedFile)
        {
            if (selectedFile == null)
            {
                throw new InvalidOperationException($"{nameof(selectedFile)} should not be null!");
            }

            if (selectedFile.IsFolder)
            {
                return;
            }

            UpdateSelectedFileList(selectedFile);
        }

        private void UpdateSelectedFileList(FilePickerItemViewModel seletedFile)
        {
            if (SelectedFiles.Contains(seletedFile))
            {
                SelectedFiles.Remove(seletedFile);
                ApplyMultiUnSelectStyle(seletedFile);
                return;
            }

            SelectedFiles.Add(seletedFile);
            ApplyMultiSelectStyle(seletedFile);
        }

        private void ApplyMultiSelectStyle(FilePickerItemViewModel selectedFile)
        {
            selectedFile.IsSelected = true;

            MultiSelectEnabled = true;
        }

        private void ApplyMultiUnSelectStyle(FilePickerItemViewModel selectedFile)
        {
            selectedFile.IsSelected = false;

            if (!SelectedFiles.Any())
            {
                MultiSelectEnabled = false;
                return;
            }

            MultiSelectEnabled = true;
        }

        public async Task ProcessSelectedFileItemActionAsync()
        {
            _fileProviderService.SelectedFileOperation = SelectedFileOperation;

            if (!HasLicenseCapabilityFor(_fileProviderService.SelectedFileOperation))
            {
                _upgradeViewModel.ShowUpgradeDialog();
                return;
            }

            if (SelectedFiles == null || !SelectedFiles.Any())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "Please select files!");
                return;
            }
            try
            {
                IEnumerable<FilePickerItemViewModel> selectedFileItems;

                selectedFileItems = SelectedFiles.Where(item => !item.IsFolder && !item.FileName.Contains(OS.Current.AxCryptExtension));
                string warningMessage = "Selected file cannot be EnCrypt,Key Share or Open";

                if (SelectedFileOperation == FileOperationOption.Decrypt)
                {
                    selectedFileItems = SelectedFiles.Where(item => !item.IsFolder && item.FileName.Contains(OS.Current.AxCryptExtension));
                    warningMessage = "Selected file cannot be Decrypt";
                }

                if (!selectedFileItems.Any())
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, warningMessage);
                    return;
                }

                await RedirectToMainScreen(selectedFileItems);
                await NavigateBackToPath(CurrentFolderID ?? "root");
                return;
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
                UpdateViewState();
            }
        }

        public async Task GoBack()
        {
            if (CurrentFolder == ROOT_DIR)
            {
                RedirectToMainScreen();
                return;
            }

            FolderItem folder = OpenedFoldersList.FirstOrDefault(ofl =>
                ofl.FileID == CurrentFolderID
            );

            if (folder == null)
            {
                return;
            }

            if (folder.ParentDirectory == ROOT_DIR)
            {
                await _fileProviderService.ListFilesAsync();
                Files = new ObservableCollection<FilePickerItemViewModel>(
                    _fileProviderService.Files
                );
                CurrentFolder = ROOT_DIR;
                CurrentFolderID = null;
                return;
            }

            if (folder.ParentDirectoryID != null)
            {
                await _fileProviderService.ListFilesAsync(folder.ParentDirectoryID);
                Files = new ObservableCollection<FilePickerItemViewModel>(
                    _fileProviderService.Files
                );

                CurrentFolder = folder.ParentDirectory;
                CurrentFolderID = folder.ParentDirectoryID;
            }
        }

        public async Task NavigateBackToPath(string folderId)
        {
            if (folderId == "root")
            {
                await _fileProviderService.ListFilesAsync();
                Files = new ObservableCollection<FilePickerItemViewModel>(
                    _fileProviderService.Files
                );
                CurrentFolder = ROOT_DIR;
                CurrentFolderID = null;
                OpenedFoldersList.Clear();
                return;
            }

            FolderItem folder = OpenedFoldersList.FirstOrDefault(ofl => ofl.FileID == folderId)!;
            if (folder == null)
            {
                return;
            }

            await _fileProviderService.ListFilesAsync(folder.FileID);
            Files = new ObservableCollection<FilePickerItemViewModel>(
                _fileProviderService.Files
            );

            int index = OpenedFoldersList.FindIndex(ofl => ofl.FileID == folderId);
            OpenedFoldersList.RemoveRange(index + 1, OpenedFoldersList.Count - (index + 1));

            CurrentFolder = folder.ParentDirectory;
            CurrentFolderID = folder.ParentDirectoryID;
        }

        private void RedirectToMainScreen()
        {
            IsVisible = false;
        }

        protected virtual async Task RedirectToMainScreen(IEnumerable<FilePickerItemViewModel> fileItems)
        {
            //try
            //{
            //    IsVisible = false;

            //    IDictionary<string, object> selectedFileDictionary = new Dictionary<string, object>
            //    {
            //        { nameof(FilePickerItemViewModel), fileItems },
            //        { nameof(FileStorageProvider), _fileProviderService }
            //    };

            //    //await New<ICloudPlatformService>().SecureFileOperationAsync(_hasPaidSubscription, selectedFileDictionary, _navigationManager);
            //}
            //catch (Exception ex)
            //{
            //    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, ex.Message);
            //    UpdateViewState();
            //}
        }

        public async Task ShareKeysAsync(EventArgs e, IEnumerable<string> selectedFiles)
        {
            await PremiumFeature_ClickAsync(
                LicenseCapability.KeySharing,
                async (ss, ee) =>
                {
                    await ShareKeyService.ShareKeysWithFileSelectionAsync(
                        _sharekeyViewModel!,
                        selectedFiles,
                        _fileOperationViewModel
                    );
                },
                null!,
                e
            );
        }

        private async Task PremiumFeature_ClickAsync(
            LicenseCapability requiredCapability,
            Func<object, EventArgs, Task> realHandler,
            object sender,
            EventArgs e
        )
        {
            if (_mainViewModel!.License.Has(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler(sender, e);
                }
                return;
            }

            _upgradeViewModel!.ShowUpgradeDialog();
        }

        private bool HasLicenseCapabilityFor(FileOperationOption fileOperationOption)
        {
            LicenseCapabilities? license = LogOnViewModel!.License;
            bool hasCapability = fileOperationOption switch
            {
                FileOperationOption.Encrypt
                    => license.Has(AxCrypt.Core.Runtime.LicenseCapability.StrongerEncryption),
                FileOperationOption.ShareKey
                    => license.Has(AxCrypt.Core.Runtime.LicenseCapability.KeySharing),
                _ => true
            };

            return hasCapability;
        }

        public void SearchFileList(string searchText)
        {
            SelectedFiles.Clear();
            if (string.IsNullOrEmpty(searchText))
            {
                Files = new ObservableCollection<FilePickerItemViewModel>(_fileProviderService.Files.Select(f => { f.IsSelected = false; return f; }));
                return;
            }

            Files = new ObservableCollection<FilePickerItemViewModel>(_fileProviderService.Files.Select(f => { f.IsSelected = false; return f; }).Where(f => f.FileName.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));
        }
    }
}