using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Main
{
    public class VaultSettingsViewModel : ViewModelBase
    {
        public LogOnViewModel LogOnViewModel { get; set; }

        public VaultSettingsViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;

            VaultEncryptDataPath = New<UserSettings>().VaultEncryptDataPath;
            AutoVaultEncryptSigninFiles = New<UserSettings>().AutoVaultEncryptSigninFiles;
            VaultEncryptWithAutoRenameFiles = New<UserSettings>().VaultEncryptWithAutoRenameFiles;
            VaultSettingsDialog = new CommonDialogService();
        }

        public string VaultEncryptDataPath { get; set; }

        private static bool _autoVaultEncryptSigninFiles;

        public bool IsVisible { get; set; }

        public bool AutoVaultEncryptSigninFiles
        {
            get => _autoVaultEncryptSigninFiles;
            set
            {
                _autoVaultEncryptSigninFiles = value;
                LogOnViewModel.UIStateChanged();
            }
        }

        private static bool _vaultEncryptionFiles;

        public bool VaultEncryptWithAutoRenameFiles
        {
            get => _vaultEncryptionFiles;
            set
            {
                _vaultEncryptionFiles = value;
                LogOnViewModel.UIStateChanged();
            }
        }

        public string ExistingVaultPath
        {
            get => New<UserSettings>().VaultEncryptDataPath;
            set => New<UserSettings>().VaultEncryptDataPath = value;
        }

        public CommonDialogService VaultSettingsDialog
        { get { return GetProperty<CommonDialogService>(nameof(VaultSettingsDialog)); } set { SetProperty(nameof(VaultSettingsDialog), value); } }

        public async Task AddSecuredFolder(EventArgs eventArgs)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, async (ss, ee) => { await WatchedFoldersAddSecureFolderMenuItem_Click(ss, ee); }, null!, eventArgs);
        }

        private async Task WatchedFoldersAddSecureFolderMenuItem_Click(object sender, EventArgs e)
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

            VaultEncryptDataPath = eventArgs.SelectedFiles.First();
        }

        public async Task SaveVaultSetting()
        {
            if (!await New<VaultSettings>().IsValidVaultPath(VaultEncryptDataPath))
            {
                VaultEncryptDataPath = New<UserSettings>().VaultEncryptDataPath;
                return;
            }

            New<UserSettings>().VaultEncryptDataPath = VaultEncryptDataPath;
            New<UserSettings>().AutoVaultEncryptSigninFiles = AutoVaultEncryptSigninFiles;

            VaultSettingsDialog.Close();

            bool onChanged = New<UserSettings>().VaultEncryptWithAutoRenameFiles != VaultEncryptWithAutoRenameFiles;
            if (onChanged)
            {
                New<UserSettings>().VaultEncryptWithAutoRenameFiles = VaultEncryptWithAutoRenameFiles;
            }
            if (onChanged && VaultEncryptWithAutoRenameFiles)
            {
                await RenameEncryptedVaultFiles();
            }
            if (onChanged && !VaultEncryptWithAutoRenameFiles)
            {
                await RestoreEncryptedVaultFiles();
            }

            await LogOnViewModel.MainViewModel.CreateVaultFolders.ExecuteAsync(VaultEncryptDataPath);
            AxCServiceProviderExtension.GetService<VaultViewModel>().RefreshVaultContainers();
        }

        public async Task MoveVaulttoSecuredfolder()
        {
            string existingVaultPath = New<UserSettings>().VaultEncryptDataPath;
            await SaveVaultSetting();

            if (!existingVaultPath.EndsWith("\\"))
            {
                existingVaultPath += "\\";
            }

            VaultFolder? vaultFolder = AxCrypt.Core.Resolve.FileSystemState.AllVaultFolders.FirstOrDefault(vf => vf.Path == existingVaultPath);
            if (vaultFolder != null)
            {
                AxCrypt.Core.Resolve.FileSystemState.RemoveVaultFolder(vaultFolder);
                await AxCrypt.Core.Resolve.FileSystemState.Save();
            }

            string newVaultPath = New<UserSettings>().VaultEncryptDataPath;

            if (existingVaultPath.Contains(newVaultPath) || newVaultPath.Contains(existingVaultPath))
            {
                return;
            }

            if (!New<IDataContainer>(existingVaultPath).IsAvailable)
            {
                return;
            }

            await LogOnViewModel.MainViewModel.AddWatchedFolders.ExecuteAsync(new[] { existingVaultPath });
        }

        public void Cancel()
        {
            VaultSettingsDialog.Close();
        }

        private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
        {
            if (LogOnViewModel.UserHas(requiredCapability))
            {
                if (realHandler != null)
                {
                    await realHandler(sender, e);
                }
                return;
            }

            AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
        }

        private async Task RenameEncryptedVaultFiles()
        {
            if (!LogOnViewModel.UserHas(LicenseCapability.Vault))
            {
                return;
            }

            string vaultEncryptPath = New<UserSettings>().VaultEncryptDataPath;
            if (vaultEncryptPath == "")
            {
                return;
            }

            IDataContainer folder = New<IDataContainer>(vaultEncryptPath);
            IEnumerable<string> encryptedVaultFiles = folder.ListEncrypted(Enumerable.Empty<IDataContainer>(), FolderOperationMode.IncludeSubfolders).Select(file => file.FullName); ;
            await New<FileOperationViewModel>().RandomRenameFiles.ExecuteAsync(encryptedVaultFiles);
        }

        private async Task RestoreEncryptedVaultFiles()
        {
            if (!LogOnViewModel.UserHas(LicenseCapability.Vault))
            {
                return;
            }

            string vaultEncryptPath = New<UserSettings>().VaultEncryptDataPath;
            if (vaultEncryptPath == "")
            {
                return;
            }

            IDataContainer folder = New<IDataContainer>(vaultEncryptPath);
            IEnumerable<string> encryptedVaultFiles = folder.ListEncrypted(Enumerable.Empty<IDataContainer>(), FolderOperationMode.IncludeSubfolders).Select(file => file.FullName); ;
            await New<FileOperationViewModel>().RestoreRandomRenameFiles.ExecuteAsync(encryptedVaultFiles);
        }
    }
}
