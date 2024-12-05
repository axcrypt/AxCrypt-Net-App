using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Code;
using AxCrypt.App.Windows.Services;
using Microsoft.UI.Windowing;
using Windows.Graphics;

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
            _appWindow.Closing += async (s, e) =>
            {
                e.Cancel = true;
                await Task.Run(() => SetupTrayIcon());
                s.Hide();
                //MauiWindowsExtensions.MinimizeToTray();
            };

            _appWindow.Changed += (s, e) =>
            {
                if (e.DidZOrderChange)
                {
                    return;
                }

                if (e.DidSizeChange)
                {
                    Window currentWindow = App.Current.Windows.FirstOrDefault();
                    if (currentWindow != null)
                    {
                        AppPreferences.MainWindowHeight = currentWindow.Height < AppPreferences.MinimumWindowHeight ? AppPreferences.MinimumWindowHeight : currentWindow.Height;
                        AppPreferences.MainWindowWidth = currentWindow.Width < AppPreferences.MinimumWindowWidth ? AppPreferences.MinimumWindowWidth : currentWindow.Width;
                        return;
                    }

                    AppPreferences.MainWindowHeight = s.ClientSize.Height;
                    AppPreferences.MainWindowWidth = s.ClientSize.Width;
                    return;
                }

                if (e.DidPositionChange)
                {
                    UpdateCurrentWindowPosition(s);
                    return;
                }

                if (e.DidPresenterChange)
                {
                    Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter = ((Microsoft.UI.Windowing.OverlappedPresenter)s.Presenter);
                    if (overlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Minimized)
                    {
                        Task.Run(() => SetupTrayIcon());
                        s.Hide();
                    }
                    if(overlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
                    {
                        UpdateCurrentWindowPosition(s);
                    }
                    return;
                }
            };
        }

        private static void UpdateCurrentWindowPosition(AppWindow s)
        {
            Window currentWindow = App.Current?.Windows.FirstOrDefault()!;
            if (currentWindow != null)
            {
                AppPreferences.MainWindowLocation = new PointInt32((int)currentWindow.X, (int)currentWindow.Y);
                return;
            }

            AppPreferences.MainWindowLocation = s.Position;
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
}
