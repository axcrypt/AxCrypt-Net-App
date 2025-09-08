using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Shared.Helpers;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace AxCrypt.App.Windows.Services;

public class WindowService : IWindowService
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

    public void RestoreWindowWithFocus()
    {
        try
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Window? window = Application.Current?.Windows.FirstOrDefault();
                if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    nint hwnd = WindowNative.GetWindowHandle(nativeWindow);

                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);

                    ITrayService tray = AxCServiceProviderExtension.GetService<ITrayService>();
                    tray?.EnsureVisible();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("RestoreWindowWithFocus error: " + ex);
        }
    }
}
