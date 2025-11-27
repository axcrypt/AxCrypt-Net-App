using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
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
        public LogOnViewModel LogOnViewModel { get; set; }
        public VaultViewModel(IStatusAlertService StatusAlerService)
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
            _statusAlertService = StatusAlerService;
        }

        private string VaultBasePath => Resolve.UserSettings.VaultEncryptDataPath ?? "";

        public string? SelectedFile { get; set; }
        public string SelectedFilePath { get; set; }
        public string SelectedFileSize { get; set; }
        public bool SelectedIsfolder { get; set; } = false;
        public string Currentfolder { get; set; }
        public string BreadcrumbPath { get; set; }
        public string VaultPath { get; set; }
        public bool IsProcessing { get; set; } = false;

        public IEnumerable<VaultItem> VaultItemList = new List<VaultItem>();
        public List<(string Name, string Path)> VaultBreadCrumb = new List<(string Name, string Path)>();

        public bool HasVaultCapability { get; set; }

        public void InitialUpdate()
        {
            Currentfolder = VaultBasePath;
        }

        public async Task LoadVaultItems()
        {

            if (string.IsNullOrEmpty(Currentfolder) || !New<IDataContainer>(Currentfolder).IsAvailable)
            {
                return;
            }

            IDataContainer vaultfolder = New<IDataContainer>(Currentfolder);
            IEnumerable<IDataStore> vaultDataStore = vaultfolder.ListOfFiles(new List<IDataContainer>(), FolderOperationMode.SingleFolder);

            IEnumerable<VaultItem> folderItems = GetFolderItems(Currentfolder);

            IEnumerable<VaultItem> fileItems = GetFileItems(vaultfolder);

            VaultItemList = folderItems.Concat(fileItems).OrderByDescending(x => x.ModifiedDate);
            CreateBreadcrums();
        }

        private IEnumerable<VaultItem> GetFolderItems(string path)
        {
            return Directory.GetDirectories(path)
                .Select(folder => new VaultItem
                {
                    Filepath = folder,
                    FileType = "folder",
                    Size = "-",
                    ModifiedDate = Directory.GetLastWriteTimeUtc(folder)
                });
        }

        private IEnumerable<VaultItem> GetFileItems(IDataContainer container)
        {
            return container.ListOfFiles(new List<IDataContainer>(), FolderOperationMode.SingleFolder)
                .Select(file => new VaultItem
                {
                    Filepath = file.FullName,
                    FileType = "file",
                    Size = GetReadableSize(file.Length()),
                    ModifiedDate = file.LastWriteTimeUtc
                });
        }


        string GetReadableSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
        }

        public void CreateBreadcrums()
        {
            VaultBreadCrumb.Clear();

            string baseDir = Path.GetDirectoryName(VaultBasePath)!;
            string relativePath = Path.GetRelativePath(baseDir, Currentfolder);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            string currentPath = baseDir;

            foreach (var part in parts)
            {
                currentPath = Path.Combine(currentPath, part);
                VaultBreadCrumb.Add((part, currentPath));
            }
        }

        public async Task FilterVaultFiles(string filename)
        {
            SelectedFile = null;

            await LoadVaultItems();
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }

            VaultItemList = VaultItemList
                .Where(f => Path.GetFileName(f.Filepath)
                    .Contains(filename, StringComparison.OrdinalIgnoreCase));

            UpdateViewState();
        }

        public async Task HandlefolderActionAsync(string action)
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
                return;

            IDataContainer dataContainer = New<IDataContainer>(SelectedFilePath);
            if (!dataContainer.IsAvailable)
            {
                _statusAlertService.Error(Texts.InvalidFolder.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                return;
            }

            switch (action)
            {
                case "Open":
                    try
                    {
                        await New<FileOperationViewModel>().OpenFiles.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Decrypt":
                    try
                    {
                        await _mainViewModel.DecryptVaultFolders.ExecuteAsync(SelectedFilePath);
                        if (!CheckActiveFiles(dataContainer.FullName))
                        {
                            _statusAlertService.Success("Folder Decryption Successfully Completed".InvariantFormat(Path.GetFileName(SelectedFilePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error("Folder Decryption failed".InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Reveal":
                    try
                    {
                        _mainViewModel.OpenSelectedFolder.Execute(SelectedFilePath);
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FolderOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;
                default:
                    break;
            }

            await LoadVaultItems();
        }

        public async Task HandlefileActionAsync(string action)
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
                return;

            IDataStore dataStore = New<IDataStore>(SelectedFilePath);
            if (!dataStore.IsAvailable)
            {
                _statusAlertService.Error(Texts.FileDoesNotExist.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                return;
            }

            switch (action)
            {
                case "Open":
                    try
                    {
                        await New<FileOperationViewModel>().OpenFiles.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Decrypt":
                    try
                    {
                        await New<FileOperationViewModel>().DecryptFiles.ExecuteAsync(new[] { SelectedFilePath });
                        if (!CheckActiveFiles(dataStore.FullName))
                        {
                            _statusAlertService.Success(Texts.FileDecryptionSuccessAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath)));
                        }
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileDecryptionFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Reveal":
                    try
                    {
                        await New<FileOperationViewModel>().ShowInFolder.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FolderOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "RenameAnonymously":
                    try
                    {
                        await New<FileOperationViewModel>().RandomRenameFiles.ExecuteAsync(new[] { SelectedFilePath });
                        if (!CheckActiveFiles(dataStore.FullName))
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
                        await New<FileOperationViewModel>().RestoreRandomRenameFiles.ExecuteAsync(new[] { SelectedFilePath });
                        if (!CheckActiveFiles(dataStore.FullName))
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
                    break;
            }

            await LoadVaultItems(); 
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

        public async Task AddVaultFolder(EventArgs eventArgs, string? folderpath = null)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(folderpath) || string.IsNullOrEmpty(VaultPath))
            {
                return;
            }

            if (New<IDataContainer>(VaultPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
                VaultPath = "";
                return;
            }

            string targetPath = await MoveVaultFolder(VaultPath, folderpath);

            if (!string.IsNullOrEmpty(targetPath))
            {
                await _mainViewModel.AddVaultFolders.ExecuteAsync(new[] { targetPath });
                Currentfolder = folderpath;
            }

            VaultPath = "";
            await LoadVaultItems();
        }

        public async Task AddVaultFiles(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFileSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(Currentfolder) || string.IsNullOrEmpty(VaultPath))
            {
                return;
            }

            await MoveVaultFile(VaultPath, Currentfolder);

            VaultPath = "";
            await LoadVaultItems();
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
        {
            if (LogOnViewModel.License.Has(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler(sender, e);
                }
                return;
            }

            LogOnViewModel.UpgradeDialog.Show();
        }

        private async Task HandleVaultFolderSelection(object sender, EventArgs e)
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs(new string[] { })
            {
                FileSelectionType = FileSelectionType.Folder
            };

            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                return;
            }

            VaultPath = eventArgs.SelectedFiles.First();
        }

        private async Task HandleVaultFileSelection(object sender, EventArgs e)
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs(new string[] { })
            {
                FileSelectionType = FileSelectionType.Encrypt
            };

            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                return;
            }

            VaultPath = eventArgs.SelectedFiles.First();
        }

        public async Task DecryptVaultfolder(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(VaultPath))
            {
                return;
            }

            if (New<IDataContainer>(VaultPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationDecryptPath);
                return;
            }

            SelectedFilePath = await MoveVaultFolder(SelectedFilePath, VaultPath);

            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                await HandlefolderActionAsync("Decrypt");
            }

            VaultPath = "";
        }

        public async Task DecryptVaultfile(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(VaultPath))
            {
                return;
            }

            while (New<IDataContainer>(VaultPath).IsVault())
            {
                await New<IPopup>().ShowAsync(
                    PopupButtons.Ok,
                    Texts.WarningTitle,
                    Texts.VaultValidationDecryptPath
                );

                await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

                if (string.IsNullOrEmpty(VaultPath))
                    return;
            }


            SelectedFilePath = await MoveVaultFile(SelectedFilePath, VaultPath);

            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                await HandlefileActionAsync("Decrypt");
            }

            VaultPath = "";
        }

        private async Task<string> MoveVaultFolder(string sourceFolderPath, string rootPath)
        {

            if (!New<IDataContainer>(rootPath).IsAvailable)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SecuredFolderValidationCannotAddItemsInVault);
                return "";
            }

            if (!CanAccessDirectory(sourceFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationAccessDenied);
                return "";
            }

            string folderName = Path.GetFileName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFolderPath = Path.Combine(rootPath, folderName);

            if (Directory.Exists(destinationFolderPath))
            {
                return "";
            }

            string sourceDrive = Path.GetPathRoot(sourceFolderPath);
            string destinationDrive = Path.GetPathRoot(rootPath);

            if (string.Equals(sourceDrive, destinationDrive, StringComparison.OrdinalIgnoreCase))
            {
                Directory.Move(sourceFolderPath, destinationFolderPath);
                return destinationFolderPath;
            }

            await CopyDirectoryAsync(sourceFolderPath, destinationFolderPath);
            Directory.Delete(sourceFolderPath, true);
            return destinationFolderPath;
        }

        private bool CanAccessDirectory(string path)
        {
            try
            {
                Directory.GetFiles(path);
                Directory.GetDirectories(path);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            Directory.CreateDirectory(destinationDir);

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                using (FileStream sourceStream = File.Open(file, FileMode.Open, FileAccess.Read))
                using (FileStream destStream = File.Create(destFile))
                {
                    await sourceStream.CopyToAsync(destStream);
                }
            }

            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                await CopyDirectoryAsync(subDir, destSubDir);
            }
        }

        private async Task<string> MoveVaultFile(string sourceFilePath, string RootPath)
        {
            string fileName = Path.GetFileName(sourceFilePath.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFilePath = Path.Combine(RootPath, fileName);

            if (File.Exists(destinationFilePath))
            {
                return "";
            }

            File.Move(sourceFilePath, destinationFilePath);
            return destinationFilePath;
        }

        public async Task CreateVaultFolder(string SelectedFilePath)
        {
            if (string.IsNullOrWhiteSpace(SelectedFilePath) || string.IsNullOrWhiteSpace(VaultPath))
                return;

            string VaultFolder = Path.Combine(SelectedFilePath, VaultPath);

            if (!Directory.Exists(VaultFolder))
            {
                Directory.CreateDirectory(VaultFolder);
            }

            VaultPath = "";
            await LoadVaultItems();
        }

        private FileSelectionEventArgs? AddedFoldersEvent { get; set; }

        public async Task EncryptDroppedFolders(IList<string> folders)
        {
            if (!folders.Any() || string.IsNullOrEmpty(Currentfolder))
            {
                return;
            }

            AddedFoldersEvent = new FileSelectionEventArgs(new string[] { })
            {
                FileSelectionType = FileSelectionType.Folder
            };

            string newFolderPath = "";
            foreach (string folder in folders)
            {
                if (New<IDataContainer>(folder).IsVault())
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
                    continue;
                }

                newFolderPath = await MoveVaultFolder(folder, Currentfolder);
                AddedFoldersEvent.SelectedFiles.Add(newFolderPath);
            }

            IsProcessing = true;
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await DragAndDroppedVaultFolderAsync(ss, ee); }, null!, AddedFoldersEvent);
            IsProcessing = false;
            await LoadVaultItems();
        }

        private async Task DragAndDroppedVaultFolderAsync(object sender, EventArgs e)
        {
            if (AddedFoldersEvent!.SelectedFiles == null || !AddedFoldersEvent.SelectedFiles.Any())
            {
                return;
            }

            await _mainViewModel.AddVaultFolders.ExecuteAsync(AddedFoldersEvent.SelectedFiles);
        }

        public async Task EncryptDroppedFile(IList<string> files)
        {
            if (!files.Any() || string.IsNullOrEmpty(Currentfolder))
            {
                return;
            }   

            AddedFoldersEvent = new FileSelectionEventArgs(new string[] { })
            {
                FileSelectionType = FileSelectionType.Folder
            };

            string newFilePath = "";
            foreach (string file in files)
            {
                if (New<IDataContainer>(file).IsVault())
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationFileDuplication);
                    continue;
                }

                newFilePath = await MoveVaultFile(file, Currentfolder);
                AddedFoldersEvent.SelectedFiles.Add(newFilePath);
            }

            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await DragAndDroppedVaultFileAsync(ss, ee); }, null!, AddedFoldersEvent);
            await LoadVaultItems();
        }

        private async Task DragAndDroppedVaultFileAsync(object sender, EventArgs e)
        {
            if (AddedFoldersEvent!.SelectedFiles == null || !AddedFoldersEvent.SelectedFiles.Any())
            {
                return;
            }

            await New<FileOperationViewModel>().EncryptFiles.ExecuteAsync(AddedFoldersEvent.SelectedFiles);
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
