using AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility;
using AxCrypt.App.Shared.CloudCore.iCloud;
using AxCrypt.App.Shared.Desktop.ViewModels.FileBrowser;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
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
        public FileProviderSelectionViewModel FileProviderViewModel;

        public bool HasEncryptionCapability { get; set; }

        public CloudDriveViewModel()
        {
            LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
            _mainViewModel = LogOnViewModel.MainViewModel;
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

        public void DisconnetCloudService(FileProviderItem provider)
        {
            switch (provider.Value)
            {
                case Core.IO.FileProvider.GoogleDrive:
                    GoogleDriveAccessInfo googleDriveAccessInfo = New<FileProvidersUserAccessInfo>().GoogleDriveAccessInfo.First();
                    New<FileProvidersUserAccessInfo>().Remove(googleDriveAccessInfo);
                    break;

                case Core.IO.FileProvider.OneDrive:
                    OneDriveAccessInfo oneDriveAccessInfo = New<FileProvidersUserAccessInfo>().OneDriveAccessInfo.First();
                    New<FileProvidersUserAccessInfo>().Remove(oneDriveAccessInfo);
                    break;

                case Core.IO.FileProvider.DropBox:
                    DropBoxAccessInfo dropBoxAccessInfo = New<FileProvidersUserAccessInfo>().DropBoxAccessInfo.First();
                    New<FileProvidersUserAccessInfo>().Remove(dropBoxAccessInfo);
                    break;

                default:
                    break;
            }
        }
    }
}