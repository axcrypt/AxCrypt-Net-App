using AxCrypt.App.Shared.CloudCore.DropBox;
using AxCrypt.App.Shared.CloudCore.GoogleDrive;
using AxCrypt.App.Shared.CloudCore.iCloud;


//using AxCrypt.App.Shared.Desktop.Core.LocalStorage;
using AxCrypt.App.Shared.CloudCore.OneDrive;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels.Authentication;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.CloudCore.CloudFileProviderUtility
{
    public class CloudFileProviderHelper
    {
        public static async Task Initialize(
            AxCrypt.Core.IO.FileProvider fileProvider,
            FilePickerViewModel filePickerViewModel,
            FileOperationOption fileOperation,
            bool hasPaidSubscription
        )
        {
            _filePickerViewModel = filePickerViewModel;
            InitiateFilePicker = (providerService) =>
                NavigateToFilePicker(providerService, fileOperation, hasPaidSubscription);
            try
            {
                switch (fileProvider)
                {
                    case AxCrypt.Core.IO.FileProvider.GoogleDrive:
                        fileProviderService = new GoogleDriveServices(InitiateFilePicker);
                        Auth = fileProviderService.OAuth2Authenticator;
                        break;

                    case AxCrypt.Core.IO.FileProvider.Local:
                        //fileProviderService = new AndroidStorageService(InitiateFilePicker);
                        Auth = fileProviderService.OAuth2Authenticator;
                        break;

                    case AxCrypt.Core.IO.FileProvider.DropBox:
                        fileProviderService = new DropBoxServices(InitiateFilePicker);
                        Auth = fileProviderService.OAuth2Authenticator;
                        break;

                    case AxCrypt.Core.IO.FileProvider.OneDrive:
                        fileProviderService = new OneDriveServices(InitiateFilePicker);
                        Auth = fileProviderService.OAuth2Authenticator;
                        break;

                    case AxCrypt.Core.IO.FileProvider.iCloud:
                        fileProviderService = new iCloudServices(InitiateFilePicker);
                        Auth = fileProviderService.OAuth2Authenticator;
                        break;

                    default:
                        break;
                }
            }
            catch (Exception exp)
            {
                await New<IPopup>()
                    .ShowAsync(
                        PopupButtons.Ok,
                        Texts.WarningTitle,
                        exp.Message,
                        Common.DoNotShowAgainOptions.None
                    );
            }
        }

        private static Action<FileStorageProvider> InitiateFilePicker { get; set; } = _ => { };

        private static FilePickerViewModel? _filePickerViewModel;

        private static void NavigateToFilePicker(
            FileStorageProvider fileProviderService,
            FileOperationOption fileOperationOption,
            bool hasPaidSubscription
        )
        {
            try
            {
                //surround with loading bar
                fileProviderService.SelectedFileOperation = fileOperationOption;

                //_filePickerViewModel = AxCServiceProvider.GetService<FilePickerViewModel>();
                _filePickerViewModel.InitializeFilePickerDialog(
                    fileProviderService,
                    hasPaidSubscription
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static async Task CompleteOAuthSelection(string authCode)
        {
            await Auth!.RaiseAuthorizedEventAsync(authCode);
        }

        public static OAuth2Auth? Auth { get; set; }

        private static FileStorageProvider fileProviderService = null;
    }
}