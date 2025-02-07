using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.Models;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.Feedback;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Windows.Components.Pages;
using AxCrypt.App.Windows.Components.Pages.Main;
using AxCrypt.App.Windows.Infrastructure;
using AxCrypt.App.Windows.Infrastructure.Dialogs;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.Services.Interface;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Cryptor.Model;
using AxCrypt.Mono;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using AxCrypt.App.Desktop.ViewModels.Secret;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Desktop.ViewModels.Notification;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Desktop;

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

            services.AddSingleton<ICssService, CssService>();

            services.AddSingleton<MainPage>();
            services.AddSingleton<Home>();
            services.AddSingleton<SecuredFolders>();
            services.AddSingleton<RecentFolders>();
            services.AddSingleton<PasswordManagerComponent>();
            services.AddSingleton<Feedback>();
            services.AddSingleton<About>();
            services.AddSingleton<Notification>();
            services.AddSingleton<Support>();

            DesktopFactory.RegisterSingletons(services);

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

            services.AddSingleton<ShareKeyViewModel>();
            services.AddSingleton<RecentFoldersViewModel>();
            services.AddSingleton<FeedbackViewModel>();
            services.AddSingleton<AboutViewModel>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<SuggestionViewModel>();
            services.AddSingleton<InviteViewModel>();
            services.AddSingleton<ImportPrivateKeyViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();
            services.AddSingleton<VerifyAccountDialogViewModel>();
            services.AddSingleton<VerifyPasswordViewModel>();

            TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());

            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        }
    }
}
