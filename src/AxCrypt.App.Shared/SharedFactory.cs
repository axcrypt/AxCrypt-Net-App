using AxCrypt.Abstractions;
using AxCrypt.App.Shared.FileOperations.IO;
using AxCrypt.App.Shared.FileOperations.Vault;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Models.Secret;
using AxCrypt.App.Shared.Providers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.UI.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.Feedback;
using AxCrypt.App.Shared.ViewModels.FileBrowser;
using AxCrypt.App.Shared.ViewModels.Notification;
using AxCrypt.App.Shared.ViewModels.Secret;
using AxCrypt.App.Shared.ViewModels.SecuredMessenger;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.Notification;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared
{
    public static class SharedFactory
    {
        private static MainViewModel? _mainViewModel;
        private static LogOnViewModel? _logOnViewModel;

        public static void RegisterSingletons(IServiceCollection services)
        {
            services.AddSingleton<ICssService, CssService>();
            services.AddSingleton<IStatusAlertService, StatusAlertService>();
            services.AddSingleton<ProcessIndicatorService>();
            services.AddSingleton<FileOperationProcessIndicatorService>();
            services.AddSingleton<ProgressBarService>();
            services.AddSingleton<FileDropService>();
            services.AddSingleton<ErrorReportService>();
            services.AddSingleton<UserService>();

            services.AddSingleton<GlobalDialogViewModel>();
            services.AddSingleton<UserPromptViewModel>();

            services.AddSingleton<LogOnViewModel>();
            services.AddSingleton<MultiFactorAuthViewModel>();
            services.AddSingleton<UpgradeVersionViewModel>();
            services.AddSingleton<UpgradeSubscriptionViewModel>();
            services.AddSingleton<RegisterViewModel>();
            services.AddSingleton<SwitchUserViewModel>();
            services.AddSingleton<AccountSetupViewModel>();

            services.AddSingleton<ShareKeyViewModel>();

            services.AddSingleton<FindFilesViewModel>();
            services.AddSingleton<VaultViewModel>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();
            services.AddSingleton<AppLocalizationOptions>();
            services.AddSingleton<SupportService>();
            services.AddSingleton<AboutViewModel>();
            services.AddSingleton<FolderSettingsViewModel>();

            services.AddSingleton<UserNotificationService>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<FeedbackViewModel>();

            services.AddSingleton<TextEncryptionViewModel>();
            services.AddSingleton<TextShareViewModel>();

            services.AddSingleton<SecretService>();

            services.AddSingleton<SecretClientModel>();
            services.AddSingleton<SecretsClientModel>();
            services.AddSingleton<NewSecretViewModel>();
            services.AddSingleton<ShareSecretViewModel>();
            services.AddScoped<ManageSecretViewModel>();
            services.AddSingleton<EditSecretViewModel>();
            services.AddSingleton<ViewSecretViewModel>();
            services.AddSingleton<ManageSecMsgrViewModel>();
            services.AddSingleton<NewSecMsgrViewModel>();

            services.AddSingleton<SecuredMessengerModel>();
            services.AddSingleton<ISecureMessagingService, SecureMessagingService>();
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<FilePickerViewModel>();
            services.AddSingleton<DropBoxAccessInfo>();
            services.AddSingleton<OneDriveAccessInfo>();
            services.AddSingleton<iCloudAccessInfo>();
            services.AddSingleton<FileProviderSelectionViewModel>();

            services.AddTransient<SecretsListViewModel>();

            TypeMap.Register.Singleton<IUpgradeVersionService>(() => new UpgradeVersionService());
            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
            TypeMap.Register.Singleton<IUserNotificationService>(() => new UserNotificationApiService());
            TypeMap.Register.Singleton<AxCryptUserAccountViewModel>(() => new AxCryptUserAccountViewModel());
        }

        public static void RegisterTypeFactories()
        {
            TypeMap.Register.New<IVaultDataStore>(() => new VaultDataStore());
            TypeMap.Register.Singleton<CustomParallelFileOperation>(() => new CustomParallelFileOperation());
            TypeMap.Register.New<VaultOperationViewModel>(() => new VaultOperationViewModel(Resolve.KnownIdentities, New<CustomParallelFileOperation>()));
            TypeMap.Register.New<IAccountSetupService>(() => new AccountSetupService());
        }

        public static void LoadUpdateCheck(MainViewModel mainViewModel, LogOnViewModel logOnViewModel)
        {
            _mainViewModel = mainViewModel;
            _logOnViewModel = logOnViewModel;
            _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await DisplayUpdateCheckPopups(); });
        }

        private static async Task DisplayUpdateCheckPopups()
        {
            if (_mainViewModel!.VersionUpdateStatus == VersionUpdateStatus.NewerVersionIsAvailable)
            {
                _logOnViewModel.UserInitiatedUpdateCheckPending = true;
            }

            await new Display().UpdateCheckPopups(_logOnViewModel.UserInitiatedUpdateCheckPending, _mainViewModel!.DownloadVersion);
            _logOnViewModel.UserInitiatedUpdateCheckPending = false;
        }
    }
}