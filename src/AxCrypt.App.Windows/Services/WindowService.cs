using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.Core.UI;
using System.Runtime.InteropServices;
using WinRT.Interop;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

public class WindowService : IWindowService
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsWindowEnabled(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;
    private const int GWL_STYLE = -16;
    private const int WS_THICKFRAME = 0x00040000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private static readonly string windowTitle = "AxCrypt file encryption";

    public void RestoreWindowWithFocus()
    {
        try
        {
            App.Current?.Dispatcher.Dispatch(() =>
            {
                IReadOnlyList<Window>? windows = Application.Current?.Windows;
                if (windows == null || windows.Count == 0) return;

                Window? mainWindow = null;
                Window? filePasswordWindow = null;

                foreach (Window w in windows)
                {
                    if (w.Title == windowTitle) filePasswordWindow = w;
                    else mainWindow = w;
                }

                HandleMainWindow(mainWindow, filePasswordWindow != null);
                HandleFilePasswordWindow(filePasswordWindow);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("RestoreWindowWithFocus error: " + ex);
        }
    }

    public void FocusFilePasswordAndMinimizeMain()
    {
        IReadOnlyList<Window>? windows = Application.Current?.Windows;
        if (windows == null || windows.Count == 0) return;

        Window? filePasswordWindow = null;
        Window? mainWindow = null;

        foreach (Window window in windows)
        {
            if (window.Title == windowTitle)
            {
                ConfigureFilePasswordWindow(window);
                filePasswordWindow = window;
            }
            else
            {
                mainWindow = window;
            }
        }

        if (filePasswordWindow != null)
            FocusWindow(filePasswordWindow);

        if (mainWindow != null)
            MinimizeWindow(mainWindow);
    }

    public bool IsMainWindowEnabled()
    {
        IntPtr? hwnd = TryGetWindowHandle(GetMainWindow());
        return hwnd.HasValue && IsWindowEnabled(hwnd.Value);
    }

    public void SetMainWindowEnabled(bool enable)
    {
        App.Current?.Dispatcher.Dispatch(() =>
        {
            IntPtr? hwnd = TryGetWindowHandle(GetMainWindow());
            if (hwnd is null) return;

            bool isCurrentlyEnabled = IsWindowEnabled(hwnd.Value);
            if (isCurrentlyEnabled != enable)
                EnableWindow(hwnd.Value, enable);
        });
    }

    private void HandleMainWindow(Window? mainWindow, bool hasFilePasswordWindow)
    {
        IntPtr? hwnd = TryGetWindowHandle(mainWindow);
        if (hwnd is null) return;

        ShowAndRestore(hwnd.Value);
        SetMainWindowEnabled(!hasFilePasswordWindow);

        if (!hasFilePasswordWindow)
            SetFocusToWindow(hwnd.Value, mainWindow);
    }

    private void HandleFilePasswordWindow(Window? filePasswordWindow)
    {
        IntPtr? hwnd = TryGetWindowHandle(filePasswordWindow);
        if (hwnd is null) return;

        ShowAndRestore(hwnd.Value);
        SetFocusToWindow(hwnd.Value, filePasswordWindow);
    }

    private static void ConfigureFilePasswordWindow(Window window)
    {
        window.MinimumWidth = window.MaximumWidth = 650;
        window.MinimumHeight = window.MaximumHeight = 400;

        window.X = (DeviceDisplay.MainDisplayInfo.Width / DeviceDisplay.MainDisplayInfo.Density - window.Width) / 2;
        window.Y = (DeviceDisplay.MainDisplayInfo.Height / DeviceDisplay.MainDisplayInfo.Density - window.Height) / 2;

        if (window.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow) return;

        IntPtr hwnd = WindowNative.GetWindowHandle(nativeWindow);
        int style = GetWindowLong(hwnd, GWL_STYLE);
        style &= ~WS_MINIMIZEBOX;
        style &= ~WS_MAXIMIZEBOX;
        style &= ~WS_THICKFRAME;
        SetWindowLong(hwnd, GWL_STYLE, style);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
    }

    private static void FocusWindow(Window? window)
    {
        try
        {
            window?.Dispatcher.Dispatch(() =>
            {
                IntPtr? hwnd = TryGetWindowHandle(window);
                if (hwnd is null) return;

                ShowWindow(hwnd.Value, SW_RESTORE);
                SetForegroundWindow(hwnd.Value);
                BringWindowToTop(hwnd.Value);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("FocusWindow error: " + ex);
        }
    }

    private void MinimizeWindow(Window window)
    {
        if (New<KnownIdentities>().IsLoggedOn)
        {
            SetMainWindowEnabled(false);
            return;
        }

        IntPtr? hwnd = TryGetWindowHandle(window);
        if (hwnd is null) return;

        ShowWindow(hwnd.Value, SW_HIDE);
    }

    private void ShowAndRestore(IntPtr hwnd)
    {
        ShowWindow(hwnd, SW_SHOW);
        ShowWindow(hwnd, SW_RESTORE);
    }

    private void SetFocusToWindow(IntPtr hwnd, Window? window)
    {
        SetForegroundWindow(hwnd);
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            nativeWindow.Activate();
    }

    private static IntPtr? TryGetWindowHandle(Window? window)
    {
        if (window?.Handler?.PlatformView is Microsoft.UI.Xaml.Window nativeWindow)
            return WindowNative.GetWindowHandle(nativeWindow);
        return null;
    }

    private static Window? GetMainWindow()
    {
        IReadOnlyList<Window>? windows = Application.Current?.Windows;
        if (windows == null) return null;

        foreach (Window w in windows)
            if (w.Title != windowTitle) return w;

        return null;
    }
}