using AxCrypt.Api.Model;
using AxCrypt.App.Desktop.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Common;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels.Home
{
    public class SubActionViewModel : ViewModelBase
    {
        private FileOperationViewModel _fileOperationViewModel;
        private MainViewModel? _mainViewModel;
        private IStatusAlertService _statusAlertService;

        public SubActionViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel = LogOnViewModel.MainViewModel;
            _fileOperationViewModel = LogOnViewModel.FileOperationViewModel;
            _statusAlertService = AxCServiceProviderExtension.StatusAlertService!;

            Initialized();
        }

        public void Initialized()
        {
            KnownFoldersViewModel = New<KnownFoldersViewModel>();

            LogOnViewModel!.BindPropertyChanged(nameof(LogOnViewModel.License), (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicyAsync(license); });
            KnownFoldersViewModel!.KnownFolders = New<IKnownFoldersDiscovery>().Discover();

            KnownFoldersViewModel!.BindPropertyChanged(nameof(KnownFoldersViewModel.KnownFolders), (IEnumerable<KnownFolder> folders) => UpdateKnownFolders(folders));
        }

        public KnownFoldersViewModel? KnownFoldersViewModel { get; set; }

        public LogOnViewModel LogOnViewModel { get; set; }

        public bool EnableCloudServices { get; set; }

        public bool EnableRandomRename { get; set; }

        public bool EnableSecureWipeFiles { get; set; }

        public bool EnableEncryptionUpgrade { get; set; }

        public bool EnableInviteUser { get; set; }

        public bool EnableAlwaysOffline { get; set; }

        public string? DisabledBackColor { get; set; }

        public SubscriptionLevel SubscriptionLevel
        {
            get
            {
                return LogOnViewModel.SubscriptionLevel;
            }
        }

        private async Task ConfigureMenusAccordingToPolicyAsync(LicenseCapabilities license)
        {
            await ConfigureCloudServiceAsync(license);
            await ConfigureAnonymousRenameAsync(license);
            await ConfigureSecureWipeAsync(license);
            await ConfigureStrongEncryptionAsync(license);
        }

        private async Task ConfigureAnonymousRenameAsync(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.RandomRename))
            {
                EnableRandomRename = true;
                EnableAlwaysOffline = false;
            }
            else
            {
                EnableRandomRename = false;
                EnableAlwaysOffline = false;
            }
        }

        private async Task ConfigureSecureWipeAsync(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.SecureWipe))
            {
                EnableSecureWipeFiles = true;
                EnableInviteUser = false;
            }
            else
            {
                EnableSecureWipeFiles = false;
                EnableInviteUser = true;
            }
        }

        private async Task ConfigureStrongEncryptionAsync(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.StrongerEncryption))
            {
                EnableEncryptionUpgrade = true;
            }
            else
            {
                EnableEncryptionUpgrade = false;
            }
        }

        private async Task ConfigureCloudServiceAsync(LicenseCapabilities license)
        {
            if (license.Has(LicenseCapability.CloudStorageAwareness))
            {
                EnableCloudServices = true;
            }
            else
            {
                EnableCloudServices = false;
            }
        }

        public async Task OnCloudServiceButtonClick(KnownFolder knownFolder)
        {
            //await _fileOperationViewModel.OpenFilesFromFolder.ExecuteAsync(knownFolder.My.FullName);
            if (!Directory.Exists(knownFolder.My.FullName))
            {
                _statusAlertService.Error("Folder is not exist");
                return;
            }

            //We are opening the Explorer not the File picker with default folder for Cloud services
            _mainViewModel.SelectedWatchedFolders = new List<string> { knownFolder.My.FullName };
            _mainViewModel.OpenSelectedFolder.Execute(_mainViewModel.SelectedWatchedFolders.First());
        }

        public async void RandomRenameAsync(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RandomRenameFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null); }, null, e);
        }

        public async void SecureWipeFiles(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.SecureWipe, async (ss, ee) => { await _fileOperationViewModel.WipeFiles.ExecuteAsync(_mainViewModel!.SelectedRecentFiles.Any() ? _mainViewModel!.SelectedRecentFiles : null); }, null, e);
        }

        public async void EncryptionUpgrade(EventArgs e)
        {
            await _fileOperationViewModel.AsyncEncryptionUpgrade.ExecuteAsync(null);
        }

        public async void InviteUser(EventArgs e)
        {
            await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { LogOnViewModel.InviteDialog.Show(); }, null, e);
        }

        public void UpgradeDialog()
        {
            LogOnViewModel.UpgradeDialog.Show();
        }

        public void AlwaysOfflineForFreeUser()
        {
            bool alwaysOnline = !New<UserSettings>().OfflineMode;
            New<UserSettings>().OfflineMode = alwaysOnline;
            New<AxCryptOnlineState>().IsOffline = alwaysOnline;

            string alert = alwaysOnline ? "Offline mode is enabled." : "Offline mode is disabled.";
            _statusAlertService.Success(alert);
            UpdateViewState();
        }

        private void UpdateKnownFolders(IEnumerable<KnownFolder> folders)
        {
            //foreach (KnownFolder folder in folders)
            //{
            //    GetIconClass(folder.My.FullName);
            //}

            UpdateViewState();
        }

        public string GetIconClass(string displayName)
        {
            if (displayName.ToLower().Contains("onedrive"))
                return "onedrv-icon";
            if (displayName.ToLower().Contains("com~apple~clouddocs"))
                return "cld-icon";
            if (displayName.ToLower().Contains("google drive"))
                return "ggldrv-icon";
            if (displayName.ToLower().Contains("dropbox"))
                return "drpbx-icon";

            return "default-icon";
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