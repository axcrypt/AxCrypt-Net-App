using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Code;
using AxCrypt.App.Windows.Services;

namespace AxCrypt.App.Windows.Infrastructure
{
    internal class AppWindowExtension
    {
        private Microsoft.UI.Windowing.AppWindow _appWindow;

        public AppWindowExtension(Microsoft.UI.Windowing.AppWindow window) 
        {
            _appWindow = window;
        }

        public void RegisterChangedEvents()
        {
            //appWindow.Closing += async (s, e) =>
            //{
            //    e.Cancel = true;
            //    //var result = await Application.Current?.MainPage?.DisplayAlert(
            //    //    "App close",
            //    //    "Do you really want to quit?",
            //    //    "Close",
            //    //    "Minimize to system tray")!;

            //    //if (result)
            //    //{
            //    //    Application.Current?.Quit();
            //    //}
            //    Task.Run(() => SetupTrayIcon());
            //    s.Hide();
            //    //MauiWindowsExtensions.MinimizeToTray();
            //};

            _appWindow.Changed += (s, e) =>
            {
                if (e.DidZOrderChange)
                {
                    return;
                }

                if (e.DidSizeChange)
                {
                    //s.OwnerWindowId
                    Window currentWindow = App.Current.Windows.FirstOrDefault();
                    if (currentWindow!=null)
                    {
                        AppPreferences.MainWindowHeight = currentWindow.Height;
                        AppPreferences.MainWindowWidth = currentWindow.Width;
                        return;
                    }

                    AppPreferences.MainWindowHeight = s.ClientSize.Height;
                    AppPreferences.MainWindowWidth = s.ClientSize.Width;
                    return;
                }

                if (e.DidPositionChange)
                {
                    AppPreferences.MainWindowLocation = s.Position;
                    return;
                }

                if (e.DidVisibilityChange)
                {
                    if (!s.IsVisible) {
                        Task.Run(() => SetupTrayIcon());
                        s.Hide();
                    }

                    return;
                }
            };
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
    }

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

}
