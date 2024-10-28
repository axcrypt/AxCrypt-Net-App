using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Infrastructure;

namespace AxCrypt.App.Windows.Services;

public class TrayService : ITrayService
{
    WindowsTrayIcon tray;

    public Action ClickHandler { get; set; }

    public void Initialize()
    {
        tray = new WindowsTrayIcon("Platforms/Windows/appicon.ico");
        tray.LeftClick = () => {
            MauiWindowsExtensions.BringToFront();
            ClickHandler?.Invoke();
        };
    }
}
