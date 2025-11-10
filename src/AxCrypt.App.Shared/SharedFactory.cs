using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.Feedback;
using AxCrypt.App.Shared.ViewModels.Notification;
using AxCrypt.App.Shared.ViewModels.Secret;
using AxCrypt.App.Shared.ViewModels.SecuredMessenger;
using AxCrypt.Common;
using AxCrypt.Core.Notification;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.App.Shared.Models.Secret;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddSingleton<FindFilesViewModel>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();

            services.AddSingleton<RegisterViewModel>();
            services.AddSingleton<AppLocalizationOptions>();

            services.AddSingleton<SecretService>();
            services.AddSingleton<SupportService>();
            services.AddSingleton<LogOnViewModel>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<FeedbackViewModel>();
            services.AddSingleton<AboutViewModel>();
            services.AddSingleton<GlobalDialogViewModel>();
            services.AddSingleton<ShareKeyViewModel>();
            services.AddSingleton<FolderSettingsViewModel>();
            services.AddSingleton<UserPromptViewModel>();

            services.AddSingleton<SecretClientModel>();
            services.AddSingleton<SecretsClientModel>();

            services.AddSingleton<NewSecretViewModel>();
            services.AddSingleton<ShareSecretViewModel>();
            services.AddScoped<ManageSecretViewModel>();
            services.AddSingleton<EditSecretViewModel>();
            services.AddSingleton<ViewSecretViewModel>();
            services.AddSingleton<UserNotificationService>();
            services.AddSingleton<NotificationViewModel>();

            services.AddSingleton<ManageSecMsgrViewModel>();
            services.AddSingleton<NewSecMsgrViewModel>();
            services.AddSingleton<SecuredMessengerModel>();

            services.AddTransient<SecretsListViewModel>();

            services.AddSingleton<ISecureMessagingService, SecureMessagingService>();
            services.AddSingleton<LogViewModel>();
            services.AddSingleton<MultiFactorAuthViewModel>();
            services.AddSingleton<UpgradeVersionViewModel>();

            TypeMap.Register.Singleton<IUpgradeVersionService>(() => new UpgradeVersionService());
            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
            TypeMap.Register.Singleton<IUserNotificationService>(() => new UserNotificationApiService());
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