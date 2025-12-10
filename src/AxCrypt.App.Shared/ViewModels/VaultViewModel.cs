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
        public string? SelectedFile { get; set; }
        public string SelectedFilePath { get; set; }
        public string SelectedFileSize { get; set; }
        public bool IsFolder { get; set; } = false;
        public string CurrentFolder { get; set; }
        public string SelectedSubFolderPath { get; set; }
        public bool IsProcessing { get; set; } = false;
        private IEnumerable<string> selectedFiles { get; set; }

        public IEnumerable<VaultItem> VaultItemList = new List<VaultItem>();

        public IList<(string Name, string Path)> VaultBreadCrumb = new List<(string Name, string Path)>();

        public void InitialUpdate()
        {
            CurrentFolder = VaultBasePath;
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

            IEnumerable<VaultItem> folderItems = GetFolderItems(CurrentFolder);
            IEnumerable<VaultItem> fileItems = GetFileItems(vaultfolder);

            VaultItemList = folderItems.Concat(fileItems).OrderByDescending(x => x.ModifiedDate);
            CreateBreadcrums();
            UpdateViewState();
        }

        private IEnumerable<VaultItem> GetFolderItems(string path)
        {
            return Directory.GetDirectories(path)
                .Select(folder => new VaultItem
                {
                    Filepath = folder,
                    FileType = "folder",
                    Size = "-",
                    ModifiedDate = Directory.GetLastWriteTimeUtc(folder).ToLocalTime()
                });
        }

        private IEnumerable<VaultItem> GetFileItems(IDataContainer container)
        {
            return container.ListOfFiles(new List<IDataContainer>(), FolderOperationMode.SingleFolder)
                .Where(file => Path.GetExtension(file.FullName).Equals(New<IRuntimeEnvironment>().AxCryptExtension, StringComparison.OrdinalIgnoreCase) && file.IsAvailable)
                .Select(file => new VaultItem
                {
                    Filepath = file.FullName,
                    FileType = "file",
                    Size = GetReadableSize(file.Length()),
                    ModifiedDate = file.LastWriteTimeUtc.ToLocalTime()
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
            string relativePath = Path.GetRelativePath(baseDir, CurrentFolder);
            string[] parts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);

            string currentPath = baseDir;

            foreach (string part in parts)
            {
                currentPath = Path.Combine(currentPath, part);
                VaultBreadCrumb.Add((part, currentPath));
            }
        }

        public void FilterVaultFiles(string filename)
        {
            SelectedFile = null;

            LoadVaultItems();
            if (string.IsNullOrWhiteSpace(filename))
            {
                return;
            }

            VaultItemList = VaultItemList
                .Where(f => Path.GetFileName(f.Filepath)
                    .Contains(filename, StringComparison.OrdinalIgnoreCase));

            UpdateViewState();
        }

        public async Task HandleFolderActionAsync(string action)
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
                        await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { SelectedFilePath });
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

            LoadVaultItems();
        }

        public async Task HandleFileActionAsync(string action)
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
                        await _fileOperationViewModel.OpenFiles.ExecuteAsync(new[] { SelectedFilePath });
                    }
                    catch (Exception ex)
                    {
                        _statusAlertService.Error(Texts.FileOpenFailedAlertMsg.InvariantFormat(Path.GetFileName(SelectedFilePath), ex.Message));
                    }
                    break;

                case "Decrypt":
                    try
                    {
                        await _fileOperationViewModel.DecryptFiles.ExecuteAsync(new[] { SelectedFilePath });
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
                        await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(new[] { SelectedFilePath });
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
                    _statusAlertService.Error($"Invalid selection action {action}");
                    break;
            }

            LoadVaultItems();
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

        public async Task AddVaultFolder(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => await HandleVaultFolderSelection(ss, ee), null!, eventArgs);

            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            if (New<IDataContainer>(SelectedSubFolderPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
                SelectedSubFolderPath = "";
                return;
            }

            string targetPath = await MoveVaultFolder(SelectedSubFolderPath, CurrentFolder);
            if (!string.IsNullOrEmpty(targetPath))
            {
                await _mainViewModel.AddVaultFolders.ExecuteAsync(new[] { targetPath });
            }

            SelectedSubFolderPath = "";
            LoadVaultItems();
        }

        public async Task AddVaultFiles(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFileSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(CurrentFolder) || !selectedFiles.Any())
            {
                return;
            }

            foreach (string filePath in selectedFiles)
            {
                await MoveVaultFile(filePath, CurrentFolder);
            }

            selectedFiles = Enumerable.Empty<string>();
            LoadVaultItems();
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
        {
            if (_logOnViewModel.License.Has(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler(sender, e);
                }
                return;
            }

            _logOnViewModel.UpgradeDialog.Show();
        }

        private async Task HandleVaultFolderSelection(object sender, EventArgs e)
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

        private async Task HandleVaultFileSelection(object sender, EventArgs e)
        {
            FileSelectionEventArgs eventArgs = new FileSelectionEventArgs([])
            {
                FileSelectionType = FileSelectionType.Encrypt
            };

            selectedFiles = Enumerable.Empty<string>();
            await New<IDataItemSelection>().HandleSelection(eventArgs);
            if (eventArgs.SelectedFiles == null || !eventArgs.SelectedFiles.Any())
            {
                return;
            }

            selectedFiles = eventArgs.SelectedFiles;
        }

        public async Task DecryptVaultfolder(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            if (New<IDataContainer>(SelectedSubFolderPath).IsVault())
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationDecryptPath);
                return;
            }

            SelectedFilePath = await MoveVaultFolder(SelectedFilePath, SelectedSubFolderPath);

            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                await HandleFolderActionAsync("Decrypt");
            }

            SelectedSubFolderPath = "";
        }

        public async Task DecryptVaultfile(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

            if (string.IsNullOrEmpty(SelectedSubFolderPath))
            {
                return;
            }

            while (New<IDataContainer>(SelectedSubFolderPath).IsVault())
            {
                await New<IPopup>().ShowAsync(
                    PopupButtons.Ok,
                    Texts.WarningTitle,
                    Texts.VaultValidationDecryptPath
                );

                await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) => { await HandleVaultFolderSelection(ss, ee); }, null!, eventArgs);

                if (string.IsNullOrEmpty(SelectedSubFolderPath))
                    return;
            }

            SelectedFilePath = await MoveVaultFile(SelectedFilePath, SelectedSubFolderPath);

            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                await HandleFileActionAsync("Decrypt");
            }

            SelectedSubFolderPath = "";
        }

        private async Task<string> MoveVaultFolder(string sourceFolderPath, string rootPath)
        {
            bool valid = await IsValidFolder(sourceFolderPath, rootPath);
            if (!valid)
            {
                return "";
            }

            string folderName = Path.GetFileName(sourceFolderPath.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFolderPath = Path.Combine(rootPath, folderName);

            if (Directory.Exists(destinationFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "The folder already exists in the destination. please rename it before moving it");
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

        private async Task<bool> IsValidFolder(string sourceFolderPath, string rootPath)
        {
            if (!New<IDataContainer>(rootPath).IsAvailable)
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SecuredFolderValidationCannotAddItemsInVault);
                return false;
            }

            if (!CanAccessDirectory(sourceFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationAccessDenied);
                return false;
            }

            if (!CanWriteDirectory(sourceFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "A file operation is in progress on this folder. Please wait for the current operation to finish.");
                return false;
            }

            if (rootPath.Contains(sourceFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, "Unable to add this folder it may contains Vault as sub folder");
                return false;
            }

            if (New<FileFilter>().IsForbiddenFolder(sourceFolderPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SystemFolderForbiddenText.InvariantFormat(sourceFolderPath));
                return false;
            }

            return true;
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

        private bool CanAccessDirectory(string folderPath)
        {
            try
            {
                string testFolder = Path.Combine(folderPath, Path.GetRandomFileName());
                Directory.CreateDirectory(testFolder);
                Directory.Delete(testFolder);

                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        private bool CanWriteDirectory(string folderPath)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        {

                        }
                    }
                    catch (IOException)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
                throw;
            }
        }

        private async Task<string> MoveVaultFile(string sourceFilePath, string RootPath)
        {
            string fileName = Path.GetFileName(sourceFilePath.TrimEnd(Path.DirectorySeparatorChar));
            string destinationFilePath = Path.Combine(RootPath, fileName);

            if (File.Exists(destinationFilePath))
            {
                PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.InformationTitle, "The file already exists in the destination. Do you want to continue with renaming?");
                if (result == PopupButtons.Cancel)
                    return "";

                FileLock Newfilepath = destinationFilePath.CreateUniqueFile();
                destinationFilePath = Newfilepath.DataStore.FullName;
            }

            File.Move(sourceFilePath, destinationFilePath, true);
            return destinationFilePath;
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
            LoadVaultItems();
        }

        private FileSelectionEventArgs? AddedFoldersEvent { get; set; }

        public async Task EncryptDroppedFolders(IEnumerable<string> folders)
        {
            if (!folders.Any() || string.IsNullOrEmpty(CurrentFolder))
            {
                return;
            }

            IsProcessing = true;
            await PremiumFeature_ClickAsync(LicenseCapability.Vault, async (ss, ee) =>
            {
                foreach (string folder in folders)
                {
                    if (New<IDataContainer>(folder).IsVault())
                    {
                        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationCannotAddVaultFldrToVault);
                        continue;
                    }

                    await MoveVaultFolder(folder, CurrentFolder);
                }
            }, null!, new FileSelectionEventArgs(folders));

            IsProcessing = false;
            LoadVaultItems();
        }

        private async Task DragAndDroppedVaultFolderAsync(object sender, EventArgs e)
        {
            if (AddedFoldersEvent!.SelectedFiles == null || !AddedFoldersEvent.SelectedFiles.Any())
            {
                return;
            }

            await _mainViewModel.AddVaultFolders.ExecuteAsync(AddedFoldersEvent.SelectedFiles);
        }

        public async Task EncryptDroppedFile(IEnumerable<string> files)
        {
            if (!files.Any() || string.IsNullOrEmpty(CurrentFolder))
            {
                return;
            }

            if (!_logOnViewModel.License.Has(LicenseCapability.Vault))
            {
                return;
            }

            string newFilePath = "";
            foreach (string file in files)
            {
                if (New<IDataContainer>(file).IsVault())
                {
                    await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.VaultValidationFileDuplication);
                    continue;
                }

                newFilePath = await MoveVaultFile(file, CurrentFolder);
            }

            LoadVaultItems();
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
