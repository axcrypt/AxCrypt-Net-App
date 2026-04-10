using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Windows.Infrastructure;

namespace AxCrypt.App.Windows.Services;

public class TrayService : ITrayService
{
    private WindowsTrayIcon? tray;

    public Action<ContextMenuItem> ClickHandler { get; set; }

    public void Initialize()
    {
        if (tray != null) return;

        tray = new WindowsTrayIcon("Resources/AppIcon/appicon.ico");
        tray.OnMenuItemClicked = (contextMenuItem) => ClickHandler?.Invoke(contextMenuItem);

        tray.EnsureVisible();
    }

    public bool Created => tray?.IsTaskbarIconCreated ?? false;

    public void Hide() => tray?.HideTrayIcon();

    public void EnsureVisible() => tray?.EnsureVisible();

    public void Remove()
    {
        tray?.HideTrayIcon();
    }

    public void Dispose()
    {
        tray?.DisposeTrayIcon();
    }
}