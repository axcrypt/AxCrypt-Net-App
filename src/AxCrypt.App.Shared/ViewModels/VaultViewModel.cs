using System;
using AxCrypt.App.Shared.FileOperations.Vault;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels
{
    public class VaultViewModel : ViewModelBase
    {
        private IStatusAlertService _statusAlertService;
        private readonly MainViewModel _mainViewModel;
        private LogOnViewModel _logOnViewModel;
        private FileOperationViewModel _fileOperationViewModel;

        public VaultViewModel(IStatusAlertService StatusAlerService)
        {
            _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
            _fileOperationViewModel = AxCServiceProviderExtension.LogOnViewModel.FileOperationViewModel;
            _statusAlertService = StatusAlerService;
        }

        private string VaultBasePath
        {
            get => Resolve.UserSettings.VaultEncryptDataPath ?? "";
        }
        public bool CreateNewFolder { get; set; }
        public string ParentFolderPath { get; set; }
        public string? SelectedFile { get; set; }
        public string SelectedFilePath { get; set; }
        public string SelectedFileSize { get; set; }
        public bool isfileselected { get; set; } = false;
        public bool IsFolder { get; set; } = false;
        public string CurrentFolder { get; set; }
        public string SelectedSubFolderPath { get; set; }
        public bool IsProcessing { get; set; } = false;
        private IEnumerable<string> _selectedFiles { get; set; }

        public IEnumerable<VaultItem> VaultItemList = new List<VaultItem>();

        public IList<(string Name, string Path)> VaultBreadCrumb = new List<(string Name, string Path)>();

        public void InitialUpdate()
        {
            CurrentFolder = VaultBasePath;
        }

        public async Task PrepareFileActionAsync(string action)
        {
            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                await HandleFileActionAsync(action);
            }
        }

        private async Task HandleFileActionAsync(string action)
        {

            switch (action)
            {
                case "Open":
                    try
                    {
                        await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Encrypt":
                    try
                    {
                        IEnumerable<IVaultDataStore> files = _selectedFiles.Select((file) => New<IVaultDataStore>().Create(file, CurrentFolder));
                        await New<VaultOperationViewModel>().EncryptFiles.ExecuteAsync(files);
                        _selectedFiles = Enumerable.Empty<string>();
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                    break;

                case "Decrypt":
                    try
                    {
                        IVaultDataStore fileStore = New<IVaultDataStore>().Create(SelectedFilePath, SelectedSubFolderPath);
                        await New<VaultOperationViewModel>().DecryptFiles.ExecuteAsync(new List<IVaultDataStore> { fileStore });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileDecryptionFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                        throw;
                    }
                    break;

                case "Reveal":
                    try
                    {
                        await _fileOperationViewModel.ShowInFolder.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FolderOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "RenameAnonymously":
                    try
                    {
                        await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(new[] { SelectedFilePath });

                        if (!CheckActiveFiles(SelectedFilePath))
                        {
                            _statusAlertService.Success(Texts.FileRenameSuccessAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileRenameFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "RenameOriginal":
                    try
                    {
                        await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(new[] { SelectedFilePath });
                        if (!CheckActiveFiles(SelectedFilePath))
                        {
                            _statusAlertService.Success(Texts.FileRestoreRenameSuccessAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileRestoreRenameFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;
                default:
                    _statusAlertService.Error($"Invalid selection action {action}");
                    break;
            }

            ResetVaultSelectionAndReload();
        }

        public void FolderActionReveal()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
                return;

            IDataContainer dataContainer = New<IDataContainer>(SelectedFilePath);
            if (!dataContainer.IsAvailable)
            {
                _statusAlertService.Error(Texts.InvalidFolder.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                return;
            }

            try
            {
                _mainViewModel.OpenSelectedFolder.Execute(SelectedFilePath);
            }
            catch (Exception ex)
            {
                _statusAlertService.Error(Texts.FolderOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
            }
        }

        public async Task AddVaultFiles()
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async () => { await HandleVaultFileSelection(); });
            if (!_selectedFiles.Any())
            {
                return;
            }

            await PrepareFileActionAsync("Encrypt");
            _selectedFiles = Enumerable.Empty<string>();
        }

        public async Task AddVaultFolder()
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async () => await HandleVaultFolderSelection());
            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            await SecureDirectoryAsync();
            SelectedSubFolderPath = "";
        }

        public async Task EncryptDroppedFolders(IEnumerable<string> folders)
        {
            if (!folders.Any() || string.IsNullOrEmpty(CurrentFolder))
            {
                return;
            }

            IsProcessing = true;
            await PremiumFeature_ClickAsync(LicenseCapability.Vault,
                async () =>
                {
                    foreach (string folder in folders)
                    {
                        SelectedSubFolderPath = folder;
                        await SecureDirectoryAsync();
                    }
                }
            );
            SelectedSubFolderPath = "";
            IsProcessing = false;
        }

        public async Task EncryptDroppedFile(IEnumerable<string> selectedfiles)
        {
            if (!selectedfiles.Any())
            {
                return;
            }

            if (!_logOnViewModel.License.Has(LicenseCapability.Vault))
            {
                return;
            }

            _selectedFiles = selectedfiles;
            await PrepareFileActionAsync("Encrypt");
        }

        public async Task DecryptVaultFileAsync()
        {
            await HandleVaultFolderSelection();
            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            while (New<IDataContainer>(SelectedSubFolderPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationDecryptPath);

                await HandleVaultFolderSelection();

                if (string.IsNullOrEmpty(SelectedSubFolderPath))
                    return;
            }

            await PrepareFileActionAsync("Decrypt");

            SelectedSubFolderPath = "";
        }

        public async Task DecryptVaultfolder()
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async () => { await HandleVaultFolderSelection(); });

            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            if (New<IDataContainer>(SelectedSubFolderPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationDecryptPath);
                return;
            }

            using (ProcessIndicator processIndicator = new ProcessIndicator())
            {
                await New<VaultOperationViewModel>().DecryptDirectoryAsync(New<IDataContainer>(SelectedFilePath), SelectedSubFolderPath);
            }

            SelectedFilePath = "";
            SelectedSubFolderPath = "";
        }

        public async Task CreateVaultFolder(string currentVaultPath)
        {
            if (string.IsNullOrEmpty(SelectedSubFolderPath))
                return;

            string newFolderPath = Path.Combine(currentVaultPath, SelectedSubFolderPath);
            IDataContainer newFolderContainer = New<IDataContainer>(newFolderPath);
            if (newFolderContainer.IsAvailable)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, $"The folder {SelectedSubFolderPath} already exist(s), please try again with different folder name!");
                return;
            }

            newFolderContainer.CreateFolder();
            CreateNewFolder = false;
            SelectedSubFolderPath = "";
        }

        public void FilterVaultFiles(string filename)
        {
            SelectedFile = null;

            LoadVaultItems();
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }

            VaultItemList = VaultItemList.Where(f =>
                Path.GetFileName(f.Filepath).Contains(filename, StringComparison.OrdinalIgnoreCase)
            );

            UpdateViewState();
        }

        public void LoadVaultItems()
        {
            if (string.IsNullOrEmpty(CurrentFolder))
            {
                return;
            }

            IDataContainer vaultfolder = New<IDataContainer>(CurrentFolder);
            if (!vaultfolder.IsAvailable)
            {
                return;
            }

            if (!CurrentFolder.Contains(VaultBasePath))
            {
                CurrentFolder = VaultBasePath;
            }

            IEnumerable<VaultItem> fileItems = GetFileItems(vaultfolder);
            IEnumerable<VaultItem> folderItems = GetFolderItems(CurrentFolder);
            VaultItemList = folderItems.Concat(fileItems).OrderByDescending(x => x.ModifiedDate);

            CreateBreadcrums();
            UpdateViewState();
        }

        private bool CheckActiveFiles(string filePath)
        {
            ActiveFile activeFile = New<FileSystemState>().FindActiveFileFromEncryptedPath(filePath);
            if (activeFile?.Status == ActiveFileStatus.AssumedOpenAndDecrypted)
            {
                return true;
            }

            return false;
        }

        private IEnumerable<VaultItem> GetFolderItems(string path)
        {
            return Directory.GetDirectories(path).Select(folder => new VaultItem
            {
                Filepath = folder,
                FileType = "folder",
                Size = "-",
                ModifiedDate = Directory.GetLastWriteTimeUtc(folder).ToLocalTime()
            });
        }

        private IEnumerable<VaultItem> GetFileItems(IDataContainer container)
        {
            string AxCryptExtension = New<IRuntimeEnvironment>().AxCryptExtension;

            return container.ListOfFiles(new List<IDataContainer>(), FolderOperationMode.SingleFolder).Where(file =>
                file.IsAvailable && Path.GetExtension(file.FullName).Equals(AxCryptExtension, StringComparison.OrdinalIgnoreCase))
                .Select(file => new VaultItem
                {
                    Filepath = file.FullName,
                    FileType = "file",
                    Size = GetReadableSize(file),
                    ModifiedDate = file.LastWriteTimeUtc.ToLocalTime()
                });
        }

        string GetReadableSize(IDataStore file)
        {
            if (!file.IsAvailable)
                return string.Empty;

            long bytes = file.Length();

            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        private void CreateBreadcrums()
        {
            VaultBreadCrumb.Clear();

            string baseDir = Path.GetDirectoryName(VaultBasePath)!;
            string relativePath = Path.GetRelativePath(baseDir, CurrentFolder);
            string[] parts = relativePath.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries
            );

            string currentPath = baseDir;

            foreach (string part in parts)
            {
                currentPath = Path.Combine(currentPath, part);
                VaultBreadCrumb.Add((part, currentPath));
            }
        }

        private void ResetVaultSelectionAndReload()
        {
            SelectedFilePath = "";
            SelectedFileSize = "";
            SelectedSubFolderPath = "";
            SelectedFile = "";
            isfileselected = false;
            VaultItemList = new List<VaultItem>();
            LoadVaultItems();
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<Task> realHandler)
        {
            if (_logOnViewModel.License.Has(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler();
                }
                return;
            }

            _logOnViewModel.UpgradeDialog.Show();
        }

        private async Task HandleVaultFolderSelection()
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs([])
            {
                FileSelectionType = FileSelectionType.Folder
            };

            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                SelectedSubFolderPath = "";
                return;
            }

            SelectedSubFolderPath = eventArgs.SelectedFiles.First();
        }

        private async Task HandleVaultFileSelection()
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs([])
            {
                FileSelectionType = FileSelectionType.Encrypt
            };

            _selectedFiles = Enumerable.Empty<string>();
            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                return;
            }

            _selectedFiles = eventArgs.SelectedFiles;
        }

        private async Task SecureDirectoryAsync()
        {
            try
            {
                IDataContainer fileSourceContainer = New<IDataContainer>(SelectedSubFolderPath);
                using (ProcessIndicator processIndicator = new ProcessIndicator())
                {
                    await New<VaultOperationViewModel>().EncryptDirectoryAsync(fileSourceContainer, CurrentFolder);
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, $"Failed to add folder(s)! {uae.Message}");
            }
            catch (Exception ex)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.MessageErrorTitle, $"Failed to add folder(s)! {ex.Message}");
            }
        }
    }

    public class VaultItem
    {
        public string Filepath { get; set; } = "";
        public string FileType { get; set; }
        public string? Size { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
