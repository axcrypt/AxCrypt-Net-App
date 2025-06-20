using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
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
            VaultEncryptionFiles = New<UserSettings>().VaultEncryptionFiles;
            VaultSettingsDialog = new CommonDialogService();
        }

        public string VaultEncryptDataPath { get; set; }

        private static bool _autoVaultEncryptSigninFiles;

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

        public bool VaultEncryptionFiles
        {
            get => _vaultEncryptionFiles;
            set
            {
                _vaultEncryptionFiles = value;
                LogOnViewModel.UIStateChanged();
            }
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
            New<UserSettings>().VaultEncryptDataPath = VaultEncryptDataPath;
            New<UserSettings>().AutoVaultEncryptSigninFiles = AutoVaultEncryptSigninFiles;
            New<UserSettings>().VaultEncryptionFiles = VaultEncryptionFiles;

            VaultSettingsDialog.Close();

            await LogOnViewModel.MainViewModel.AddWatchedFolders.ExecuteAsync(VaultEncryptDataPath);
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
    }
}
