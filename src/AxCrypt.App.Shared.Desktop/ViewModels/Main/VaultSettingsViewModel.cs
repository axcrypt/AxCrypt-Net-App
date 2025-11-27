using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.IO;
using AxCrypt.Core.Runtime;
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
            if (New<FileFilter>().IsForbiddenFolder(VaultEncryptDataPath))
            {
                await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.WarningTitle, Texts.SystemFolderForbiddenText.InvariantFormat(VaultEncryptDataPath));
                return;
            }

            New<UserSettings>().VaultEncryptDataPath = VaultEncryptDataPath;
            New<UserSettings>().AutoVaultEncryptSigninFiles = AutoVaultEncryptSigninFiles;
            New<UserSettings>().VaultEncryptWithAutoRenameFiles = VaultEncryptWithAutoRenameFiles;

            VaultSettingsDialog.Close();

            if (New<UserSettings>().VaultEncryptWithAutoRenameFiles)
            {
                await RenameEncryptedVaultFiles();
            }
            else
            {
                await RestoreEncryptedVaultFiles();
            }

            await LogOnViewModel.MainViewModel.CreateVaultFolders.ExecuteAsync(VaultEncryptDataPath);
        }

        public async Task MoveVaulttoSecuredfolder()
        {
            string existingVaultPath = New<UserSettings>().VaultEncryptDataPath;
            await SaveVaultSetting();
            await LogOnViewModel.MainViewModel.AddWatchedFolders.ExecuteAsync(new[] { existingVaultPath });
        }

        public void Cancel()
        {
            VaultSettingsDialog.Close();
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

        private async Task RenameEncryptedVaultFiles()
        {
            if (!LogOnViewModel.License.Has(LicenseCapability.Vault))
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
            if (!LogOnViewModel.License.Has(LicenseCapability.Vault))
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
