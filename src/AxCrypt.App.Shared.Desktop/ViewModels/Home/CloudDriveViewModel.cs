using AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility;
using AxCrypt.App.Shared.CloudCore.iCloud;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Desktop.ViewModels.FileBrowser;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home
{
    public class CloudDriveViewModel : ViewModelBase
    {
        private MainViewModel? _mainViewModel;
        private PaidFeaturegateService? _paidGateService;
        private DesktopFilePickerViewModel? _filePickerVm;
        public FileProviderSelectionViewModel FileProviderViewModel;

        public bool HasEncryptionCapability { get; set; }

        public CloudDriveViewModel()
        {
            LogOnViewModel    = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel    = LogOnViewModel.MainViewModel;
            _paidGateService  = AxCServiceProviderExtension.GetService<PaidFeaturegateService>();
            _filePickerVm     = AxCServiceProvider.GetService<DesktopFilePickerViewModel>();
            FileProviderViewModel = AxCServiceProvider.GetService<FileProviderSelectionViewModel>();
            _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { if (_mainViewModel.LoggedOn) ConfigureMenusAccordingToPolicy(license); });

            InitializeData();
        }

        public void InitializeData()
        {
            FileProviderViewModel.UpdateFileProviderSelection(FileOperationOption.None, InitializeProviderFileSelection);
        }

        private async Task InitializeProviderFileSelection()
        {
            if (FileProviderViewModel.SelectedFileProvider == null)
            {
                return;
            }

            FilePickerViewModel filePickerViewModel = AxCServiceProvider.GetService<DesktopFilePickerViewModel>();
            await CloudFileProviderHelper.Initialize(FileProviderViewModel.SelectedFileProvider.Value, filePickerViewModel, FileProviderViewModel.SelectedFileOperation, HasEncryptionCapability);
        }

        public LogOnViewModel LogOnViewModel { get; set; }
        public bool EnableCloudServices { get; set; }

        private void ConfigureMenusAccordingToPolicy(LicenseCapabilities license)
        {
            ConfigureCloudService(license);

            UpdateViewState();
        }

        private void ConfigureCloudService(LicenseCapabilities license)
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

        public bool IsConnected(FileProviderItem provider)
        {
            switch (provider.Value)
            {
                case Core.IO.FileProvider.GoogleDrive:
                    return New<FileProvidersUserAccessInfo>().GoogleDriveAccessInfo?.Any() ?? false;

                case Core.IO.FileProvider.OneDrive:
                    return New<FileProvidersUserAccessInfo>().OneDriveAccessInfo?.Any() ?? false;

                case Core.IO.FileProvider.DropBox:
                    return New<FileProvidersUserAccessInfo>().DropBoxAccessInfo?.Any() ?? false;

                case Core.IO.FileProvider.iCloud:
                    return New<IICloudPlatformFileAccess>().IsAvailable;

                default:
                    return false;
            }
        }

        // ── Upgrade prompt ─────────────────────────────────────
        /// <summary>Shows the paid-gate popup for the Cloud Services feature.</summary>
        public void ShowUpgradePopup()
        {
            _paidGateService?.ShowPaidGate(
                Texts.SecuredCloudLinkLabel,
                Texts.SecuredCloudHelpText,
                new[] { Texts.SecureCloudFileProtectionPopup, Texts.KeepCloudDataPrivatePopup, Texts.UnlockAdvancedCloudSecurityPopup });
        }

        // ── Row action ─────────────────────────────────────────
        /// <summary>
        /// Handles a cloud-drive row click.
        /// Runs the auth / selection flow and returns the route the razor should navigate to,
        /// or null when no navigation is required (auth cancelled / failed).
        /// </summary>
        public async Task<string?> HandleRowActionAsync(FileProviderItem provider)
        {
            await FileProviderViewModel.SubActionSelectProvider(provider);

            // Always suppress the legacy modal — the nav page is the home for cloud browsing.
            if (_filePickerVm != null)
            {
                _filePickerVm.IsVisible = false;
            }

            return IsConnected(provider) ? "/cloudbrowser" : null;
        }

        public void DisconnetCloudService(FileProviderItem provider)
        {
            switch (provider.Value)
            {
                case Core.IO.FileProvider.GoogleDrive:
                    GoogleDriveAccessInfo? googleDriveAccessInfo = New<FileProvidersUserAccessInfo>().GoogleDriveAccessInfo.FirstOrDefault();
                    New<FileProvidersUserAccessInfo>().Remove(googleDriveAccessInfo!);
                    break;

                case Core.IO.FileProvider.OneDrive:
                    OneDriveAccessInfo? oneDriveAccessInfo = New<FileProvidersUserAccessInfo>().OneDriveAccessInfo.FirstOrDefault();
                    New<FileProvidersUserAccessInfo>().Remove(oneDriveAccessInfo!);
                    break;

                case Core.IO.FileProvider.DropBox:
                    DropBoxAccessInfo? dropBoxAccessInfo = New<FileProvidersUserAccessInfo>().DropBoxAccessInfo.FirstOrDefault();
                    New<FileProvidersUserAccessInfo>().Remove(dropBoxAccessInfo!);
                    break;

                default:
                    break;
            }
        }
    }
}