using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core;
using Microsoft.UI.Windowing;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Infrastructure
{
    internal class AppWindowExtension
    {
        private static Microsoft.UI.Windowing.AppWindow? _appWindow = null;

        public AppWindowExtension(Microsoft.UI.Windowing.AppWindow window)
        {
            if (_appWindow == null)
            {
                _appWindow = window;
            }
        }

        public void RegisterChangedEvents()
        {
            if (_appWindow == null)
            {
                return;
            }

            _appWindow.Closing += (s, e) =>
            {
                LogOnViewModel logOnService = AxCServiceProviderExtension.LogOnViewModel!;
                if (logOnService.IsLoggedOn)
                {
                    e.Cancel = true;
                    SetupTrayIcon();
                    Resolve.UserSettings.RestoreFullWindow = false;
                    s.Hide();
                }
            };

            _appWindow.Changed += (s, e) =>
            {
                if (e.DidZOrderChange)
                {
                    return;
                }

                if (e.DidSizeChange)
                {
                    Window? currentWindow = App.Current!.Windows.FirstOrDefault();
                    if (currentWindow != null)
                    {
                        AppPreferences.MainWindowHeight = currentWindow.Height < AppPreferences.MinimumWindowHeight ? AppPreferences.MinimumWindowHeight : currentWindow.Height;
                        AppPreferences.MainWindowWidth = currentWindow.Width < AppPreferences.MinimumWindowWidth ? AppPreferences.MinimumWindowWidth : currentWindow.Width;
                        currentWindow.Height = AppPreferences.MainWindowHeight;
                        currentWindow.Width = AppPreferences.MainWindowWidth;
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
                        SetupTrayIcon();
                        Resolve.UserSettings.RestoreFullWindow = false;
                        s.Hide();
                    }
                    if (overlappedPresenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
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
                AppPreferences.MainWindowLocation = new System.Drawing.Point((int)currentWindow.X, (int)currentWindow.Y);
                return;
            }

            AppPreferences.MainWindowLocation = new System.Drawing.Point((int)s.Position.X, (int)s.Position.Y);
        }

        private static void SetupTrayIcon()
        {
            ITrayService trayService = AxCServiceProviderExtension.GetService<ITrayService>();
            if (trayService != null)
            {
                trayService.Initialize();

                Task.Run(() =>
                {
                    New<INotificationService>()?.ShowNotification("AxCrypt File Encryption", "Click here to restore the window");
                });

                trayService.ClickHandler = () =>
                    RestoreWindowWithFocus();
            }
        }

        private static void RestoreWindowWithFocus()
        {
            Task.Run(() =>
            {
                if (_appWindow == null)
                {
                    throw new ArgumentNullException(nameof(_appWindow));
                }

                Resolve.UserSettings.RestoreFullWindow = true;
                _appWindow.Show(true);
                _appWindow.SetPresenter(AppWindowPresenterKind.Default);
                Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter = ((Microsoft.UI.Windowing.OverlappedPresenter)_appWindow.Presenter);
                overlappedPresenter.Restore(true);
            });
        }
    }
}