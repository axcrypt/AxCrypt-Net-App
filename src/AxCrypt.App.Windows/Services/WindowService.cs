using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.UI;
using System.Runtime.InteropServices;
using WinRT.Interop;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

public class WindowService : IWindowService
{
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const int SW_MINIMIZE = 6;
    private readonly string windowTitle = "File Password";

    public void RestoreWindowWithFocus()
    {
        try
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                Window? window = Application.Current?.Windows.FirstOrDefault(w => w.Title != windowTitle);
                if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
                {
                    nint hwnd = WindowNative.GetWindowHandle(nativeWindow);
                    ShowWindow(hwnd, SW_SHOW);
                    ShowWindow(hwnd, SW_RESTORE);
                    SetForegroundWindow(hwnd);

                    nativeWindow.Activate();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("RestoreWindowWithFocus error: " + ex);
        }
    }

    private static IntPtr GetHandle(Window window)
    {
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
        {
            return WindowNative.GetWindowHandle(nativeWindow);
        }

        return IntPtr.Zero;
    }

    private static void FocusWindow(Window? window)
    {
        try
        {
            window.Dispatcher.Dispatch(() =>
            {
                IntPtr hWnd = GetHandle(window);
                if (hWnd == IntPtr.Zero) return;

                ShowWindow(hWnd, SW_RESTORE);

                SetForegroundWindow(hWnd);
                BringWindowToTop(hWnd);
            });
        }
        catch (Exception ex)
        {

        }
    }

    private static void MinimizeWindow(Window window)
    {
        if (New<KnownIdentities>().IsLoggedOn)
        {
            return;
        }

        IntPtr hWnd = GetHandle(window);
        if (hWnd == IntPtr.Zero) return;

        ShowWindow(hWnd, SW_HIDE);
    }

    public void FocusFilePasswordAndMinimizeMain()
    {
        IReadOnlyList<Window>? windows = Application.Current?.Windows;

        if (windows == null || windows.Count == 0) return;

        Window? filePasswordWindow = null;
        Window? mainWindow = null;

        foreach (Window window in windows)
        {
            if (window.Title == "File Password")
            {
                filePasswordWindow = window;
            }
            else
            {
                mainWindow = window;
            }
        }

        if (filePasswordWindow != null)
        {
            FocusWindow(filePasswordWindow);
        }

        if (mainWindow != null)
        {
            MinimizeWindow(mainWindow);
        }
    }
}
