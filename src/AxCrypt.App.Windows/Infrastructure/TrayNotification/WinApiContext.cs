using System.Runtime.InteropServices;

namespace AxCrypt.App.Windows.Infrastructure.TrayNotification;

internal static class WinApiContext
{
    public const int TPM_LEFTALIGN = 0x0000;
    public const int TPM_RIGHTALIGN = 0x0008;
    public const int TPM_TOPALIGN = 0x0000;
    public const int TPM_BOTTOMALIGN = 0x0020;
    public const int TPM_RETURNCMD = 0x0100;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hwnd, IntPtr lprc);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindow(IntPtr hWnd);
}
