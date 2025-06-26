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
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using AxCrypt.Cryptor.Model;
using Microsoft.Extensions.DependencyInjection;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared
{
    public static class SharedFactory
    {
        private static MainViewModel? _mainViewModel;

        private static bool _userInitiatedUpdateCheckPending = false;

        public static void RegisterSingletons(IServiceCollection services)
        {
            services.AddSingleton<ICssService, CssService>();
            services.AddSingleton<IStatusAlertService, StatusAlertService>();
            services.AddSingleton<ProcessIndicatorService>();
            services.AddSingleton<ProgressBarService>();
            services.AddSingleton<FileDropService>();

            services.AddSingleton<FileDetails>();
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

            services.AddSingleton<SecretClientModel>();
            services.AddSingleton<SecretsClientModel>();

            services.AddSingleton<NewSecretViewModel>();
            services.AddSingleton<ShareSecretViewModel>();
            services.AddSingleton<ManageSecretViewModel>();
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
            services.AddSingleton<TwoFactorAuthViewModel>();

            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        }

        public static void LoadUpdateCheck(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { if (loggedOn) await mainViewModel.AxCryptUpdateCheck.ExecuteAsync(New<UserSettings>().LastUpdateCheckUtc); });
            _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { if (_mainViewModel.LoggedOn) _userInitiatedUpdateCheckPending = true; await DisplayUpdateCheckPopups(); });
        }

        private static async Task DisplayUpdateCheckPopups()
        {
            await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel!.DownloadVersion);
            _userInitiatedUpdateCheckPending = false;
        }
    }
}