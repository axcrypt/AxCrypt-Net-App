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
using AxCrypt.App.Windows.Components.Pages.Shared;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Core;
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
        public static MauiApp CreateMauiApp(CommandLine commandLine)
        {
            var builder = MauiApp.CreateBuilder();
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
#if WINDOWS
                //lifecycle
                //    .AddWindows(windows =>
                //        windows.OnNativeMessage((app, args) => {
                //            if (WindowExtensions.Hwnd == IntPtr.Zero)
                //            {
                //                WindowExtensions.Hwnd = args.Hwnd;
                //                WindowExtensions.SetIcon("Platforms/Windows/trayicon.ico");
                //            }
                //        }));

                lifecycle.AddWindows(windows =>
                {
                    windows.OnWindowCreated((del) =>
                    {
                        del.ExtendsContentIntoTitleBar = true;
                    });


                    windows.OnVisibilityChanged((vis, fd) =>
                    {
                        // when minimized - vis.Visible = false
                    });

                    windows.OnClosed((wind, windArg) =>
                    {
                        wind.AppWindow.Hide();
                        windArg.Handled = true;
                        //MauiWindowsExtensions.MinimizeToTray();
                        //MauiWindowsExtensions.BringToFront();
                        //    SetupTrayIcon();
                    });

                    //windows.OnVisibilityChanged((del) =>
                    //{
                    //    //del.AppWindow.
                    //});


                }
                );
#endif
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
            services.AddSingleton<FileShareService>();

            services.AddSingleton<ITrayService, TrayService>();
            services.AddSingleton<INotificationService, NotificationService>();
            
            services.AddSingleton<ILogging, Logging>();
            services.AddSingleton<IStatusChecker, StatusChecker>();
            services.AddSingleton<IFolderPicker, FolderPickerWindows>();
            services.AddSingleton<IExportKeyManagementFile, ExportKeyManagementFile>();
            services.AddSingleton<AppLocalizationOptions>();

            services.AddSingleton<LoginModel>();
            services.AddSingleton<MainPage>();
            services.AddSingleton<Home>();
            services.AddSingleton<HomeBody>();
            services.AddSingleton<HomeBody>();
            services.AddSingleton<MainHomeComponent>();
            services.AddSingleton<SecuredFolders>();
            services.AddSingleton<RecentFolders>();
            services.AddSingleton<PasswordManager>();
            services.AddSingleton<Feedback>();
            services.AddSingleton<About>();
            services.AddSingleton<Notification>();
            services.AddSingleton<TopMenu>();
            services.AddSingleton<Support>();
            services.AddSingleton<SettingsDeskPopup>();
            services.AddSingleton<NotificationPopup>();
            services.AddSingleton<AccountDeskPopup>();
            services.AddSingleton<LoginService>();
            services.AddSingleton<SupportService>();
            services.AddSingleton<SecretClientModel>();
            services.AddSingleton<SecretsClientModel>();
            services.AddSingleton<NewSecretViewModel>();
            services.AddSingleton<SecretService>();
            services.AddSingleton<ShareSecretViewModel>();
            services.AddSingleton<ManageSecretViewModel>();
            services.AddSingleton<EditSecretViewModel>();
            services.AddSingleton<ViewSecretViewModel>();
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<SecretsListViewModel>();
            services.AddSingleton<SecretServiceUtility>();
            services.AddSingleton<UserNotificationService>();
            services.AddSingleton<HomeViewModel>();

            services.AddSingleton<HomeBodyViewModel>();
            services.AddSingleton<RecentFilesViewModel>();
            services.AddSingleton<ShareKeyViewModel>();
            services.AddSingleton<RecentFoldersViewModel>();
            services.AddSingleton<FeedbackViewModel>();
            services.AddSingleton<AboutViewModel>();
            services.AddSingleton<SupportViewModel>();
            services.AddSingleton<SuggestionViewModel>();
            services.AddSingleton<InviteViewModel>();
            services.AddSingleton<TopMenuViewModel>();
            services.AddSingleton<AccountViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<AdvancedOptionsViewModel>();
            services.AddSingleton<RecentFilesViewModel>();

            services.AddSingleton<HomeUserService>();

            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        }
    }
}
