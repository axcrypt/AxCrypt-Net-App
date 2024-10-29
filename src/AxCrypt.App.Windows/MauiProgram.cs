using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace AxCrypt.App.Windows
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            IServiceCollection services = builder.Services;
            services.AddMauiBlazorWebView();

            RegisterSingletons(services);

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
            services.AddSingleton<ITrayService, TrayService>();
            services.AddSingleton<INotificationService, NotificationService>();
            services.AddSingleton<HomeViewModel>();

        }
    }
}
