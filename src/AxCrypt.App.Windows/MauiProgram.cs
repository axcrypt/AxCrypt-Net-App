using AxCrypt.Abstractions;
using AxCrypt.App.Components.Helpers;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Models.Notification;
using AxCrypt.App.Components.Models.Secret;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Components.ViewModels;
using AxCrypt.App.Components.ViewModels.Feedback;
using AxCrypt.App.Windows.Components.Pages;
using AxCrypt.App.Windows.Components.Pages.Main;
using AxCrypt.App.Windows.Components.Pages.Password;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor.Model;
using AxCrypt.Mono;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace AxCrypt.App.Windows
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            IServiceCollection services = builder.Services;
            RegisterSingletons(services);

            services.AddSingleton<ICustomNavigationService, CustomNavigationService>();

            services.AddMauiBlazorWebView();

            builder.ConfigureLifecycleEvents(lifecycle =>
            {
                lifecycle.AddWindows(lifecycleBuilder => lifecycleBuilder.OnWindowCreated(window =>
                {
                    window.ExtendsContentIntoTitleBar = true;

                    nint handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    Microsoft.UI.WindowId id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                    new AppWindowExtension(appWindow).RegisterChangedEvents();
                }));
            });

#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void RegisterSingletons(IServiceCollection services)
        {
            services.AddSingleton<IStatusAlertService, StatusAlertService>();
            services.AddSingleton<ProcessIndicatorService>();
            services.AddSingleton<ProgressBarService>();

            services.AddSingleton<ITrayService, TrayService>();
            services.AddSingleton<INotificationService, NotificationService>();

            services.AddSingleton<ILogging, Logging>();
            services.AddSingleton<IStatusChecker, StatusChecker>();
            services.AddSingleton<IFolderPicker, FolderPickerWindows>();
            services.AddSingleton<IExportKeyManagementFile, ExportKeyManagementFile>();
            services.AddSingleton<AppLocalizationOptions>();

            services.AddSingleton<ICssService, CssService>();

            services.AddSingleton<MainPage>();
            services.AddSingleton<Home>();
            services.AddSingleton<HomeActionsComponent>();
            services.AddSingleton<SecuredFolders>();
            services.AddSingleton<RecentFolders>();
            services.AddSingleton<PasswordManager>();
            services.AddSingleton<Feedback>();
            services.AddSingleton<About>();
            services.AddSingleton<Notification>();
            services.AddSingleton<TopMenu>();
            services.AddSingleton<Support>();
            services.AddSingleton<AppSettingsComponent>();
            services.AddSingleton<NotificationPopup>();

            services.AddSingleton<LogOnViewModel>();
            services.AddSingleton<RegisterViewModel>();

            services.AddSingleton<ProfileOptionComponent>();
            services.AddSingleton<SupportService>();
            services.AddSingleton<SecretClientModel>();
            services.AddSingleton<SecretsClientModel>();
            services.AddSingleton<NewSecretViewModel>();
            services.AddSingleton<SecretService>();
            services.AddSingleton<ShareSecretViewModel>();
            services.AddSingleton<ManageSecretViewModel>();
            services.AddSingleton<EditSecretViewModel>();
            services.AddSingleton<ViewSecretViewModel>();
            services.AddSingleton<UserNotificationService>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<SecretsListViewModel>();
            services.AddSingleton<SecretServiceUtility>();

            services.AddSingleton<HomeActionsViewModel>();
            services.AddSingleton<RecentFilesViewModel>();
            services.AddSingleton<ShareKeyViewModel>();
            services.AddSingleton<RecentFoldersViewModel>();
            services.AddSingleton<FeedbackViewModel>();
            services.AddSingleton<AboutViewModel>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<SuggestionViewModel>();
            services.AddSingleton<InviteViewModel>();
            services.AddSingleton<TopMenuViewModel>();
            services.AddSingleton<ProfileViewModel>();
            services.AddSingleton<AppSettingsViewModel>();
            services.AddSingleton<AdvancedOptionsViewModel>();
            services.AddSingleton<RecentFilesViewModel>();
            services.AddSingleton<ImportPrivateKeyViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();
            services.AddSingleton<VerifyAccountDialogViewModel>();
            services.AddSingleton<VerifyPasswordViewModel>();

            TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());

            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        }
    }
}
