using System.Runtime.InteropServices;
using WinRT.Interop;

namespace AxCrypt.App.Windows.Infrastructure;

/// <summary>
/// Forces the new instance to the front after a Switch User / Clear
/// Settings restart. Windows blocks foreground-stealing by default, so
/// even after AllowSetForegroundWindow the new window often lands in
/// the taskbar without coming forward. This helper drives the full
/// SW_RESTORE → SetForegroundWindow → BringWindowToTop sequence after
/// the main window is ready.
/// </summary>
internal static class RestartFocusHelper
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    private const int SW_SHOW = 5;
    private const int SW_RESTORE = 9;

    /// <summary>
    /// True if the process was launched with "--restart" by UIThread.RestartApplication.
    /// </summary>
    public static bool IsRestartLaunch =>
        Environment.GetCommandLineArgs().Any(a => string.Equals(a, "--restart", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Force the supplied MAUI window to the front. Safe to call repeatedly.
    /// </summary>
    public static void ForceForeground(Microsoft.Maui.Controls.Window? window)
    {
        if (window?.Handler?.PlatformView is not Microsoft.UI.Xaml.Window nativeWindow)
        {
            return;
        }

        try
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(nativeWindow);
            ShowWindow(hwnd, SW_SHOW);
            ShowWindow(hwnd, SW_RESTORE);

            // Anti-stealing dance: attach this thread's input queue to the
            // foreground window's thread, then SetForegroundWindow succeeds.
            IntPtr fg = GetForegroundWindow();
            uint fgThread = GetWindowThreadProcessId(fg, out _);
            uint thisThread = GetCurrentThreadId();
            bool attached = false;
            if (fgThread != 0 && fgThread != thisThread)
            {
                attached = AttachThreadInput(thisThread, fgThread, true);
            }
            try
            {
                SetForegroundWindow(hwnd);
                BringWindowToTop(hwnd);
            }
            finally
            {
                if (attached) AttachThreadInput(thisThread, fgThread, false);
            }
        }
        catch
        {
            // Focus best-effort. Worst case the user clicks the taskbar.
        }
    }
}
