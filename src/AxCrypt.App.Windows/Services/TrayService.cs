using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Infrastructure;

namespace AxCrypt.App.Windows.Services;

public class TrayService : ITrayService
{
    WindowsTrayIcon? tray;

    public Action<ContextMenuItem> ClickHandler { get; set; }

    public void Initialize()
    {
        tray = new WindowsTrayIcon("Resources/AppIcon/appicon.ico");
        tray.OnMenuItemClicked = (contextMenuItem) =>
        {
            switch (contextMenuItem)
            {
                case ContextMenuItem.Advanced:
                    ClickHandler?.Invoke(ContextMenuItem.Advanced);
                    break;
                case ContextMenuItem.SignOut:
                    ClickHandler?.Invoke(ContextMenuItem.SignOut);
                    break;
                case ContextMenuItem.Exit:
                    ClickHandler?.Invoke(ContextMenuItem.Exit);
                    break;
            }
        };
    }

    public void Dispose()
    {
        tray?.DisposeTrayIcon();
    }
}
