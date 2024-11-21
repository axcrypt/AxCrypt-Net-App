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
using AxCrypt.App.Windows.Helpers;
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
                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                    appWindow.Closing += async (s, e) =>
                    {
                        e.Cancel = true;
                        //var result = await Application.Current?.MainPage?.DisplayAlert(
                        //    "App close",
                        //    "Do you really want to quit?",
                        //    "Close",
                        //    "Minimize to system tray")!;

                        //if (result)
                        //{
                        //    Application.Current?.Quit();
                        //}
                        Task.Run(() => SetupTrayIcon());
                        s.Hide();
                        //MauiWindowsExtensions.MinimizeToTray();
                    };
                }));
            });

            //            builder.ConfigureLifecycleEvents(lifecycle =>
            //            {
            //#if WINDOWS
            //                //lifecycle
            //                //    .AddWindows(windows =>
            //                //        windows.OnNativeMessage((app, args) => {
            //                //            if (WindowExtensions.Hwnd == IntPtr.Zero)
            //                //            {
            //                //                WindowExtensions.Hwnd = args.Hwnd;
            //                //                WindowExtensions.SetIcon("Platforms/Windows/trayicon.ico");
            //                //            }
            //                //        }));

            //                lifecycle.AddWindows(windows =>
            //                {
            //                    windows.OnWindowCreated((del) =>
            //                    {
            //                        del.ExtendsContentIntoTitleBar = true;
            //                    });


            //                    windows.OnVisibilityChanged((vis, fd) =>
            //                    {
            //                        // when minimized - vis.Visible = false
            //                        if (vis.Visible == false)
            //                        {
            //                            vis.AppWindow.Hide();
            //                            fd.Handled = true;
            //                            //MauiWindowsExtensions.MinimizeToTray();
            //                            //MauiWindowsExtensions.BringToFront();
            //                            SetupTrayIcon();
            //                        }
            //                    });

            //                    windows.OnClosed((wind, windArg) =>
            //                    {
            //                        wind.AppWindow.Hide();
            //                        windArg.Handled = true;
            //                        MauiWindowsExtensions.MinimizeToTray();
            //                        //MauiWindowsExtensions.BringToFront();
            //                        SetupTrayIcon();
            //                    });

            //                    //windows.OnVisibilityChanged((del) =>
            //                    //{
            //                    //    //del.AppWindow.
            //                    //});


            //                }
            //                );
            //#endif
            //            });

#if DEBUG
            services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        private static void SetupTrayIcon()
        {
            ITrayService trayService = new TrayService();
            if (trayService != null)
            {
                trayService.Initialize();

                INotificationService notificationService = new NotificationService();
                notificationService
                        ?.ShowNotification("AxCrypt File Encryption", "Click here to restore the window");

                trayService.ClickHandler = () =>
                    notificationService
                        ?.ShowNotification("AxCrypt File Encryption", "Click here to restore the window");
            }
        }

        private static void RegisterSingletons(IServiceCollection services)
        {
            services.AddSingleton<IStatusAlertService, StatusAlertService>();
            services.AddSingleton<ProcessIndicatorService>();

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
            services.AddSingleton<NotificationViewModel>();
            services.AddSingleton<NotificationItemViewModel>();
            services.AddSingleton<SecretsListViewModel>();
            services.AddSingleton<SecretServiceUtility>();
            services.AddSingleton<UserNotificationService>();

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
            services.AddSingleton<CreateNewAccountDialogViewModel>();
            services.AddSingleton<FilePasswordDialogViewModel>();
            services.AddSingleton<VerifyAccountDialogViewModel>();
            services.AddSingleton<VerifyPasswordViewModel>();

            services.AddSingleton<HomeUserService>();

            TypeMap.Register.Singleton<IVerifySignInPassword>(() => new VerifySignInPassword());

            TypeMap.Register.Singleton<AccountStatusViewModel>(() => new AccountStatusViewModel());
        }
    }
}
