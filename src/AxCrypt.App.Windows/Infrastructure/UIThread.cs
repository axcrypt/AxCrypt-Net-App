using AxCrypt.Core.Runtime;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AxCrypt.App.Windows.Infrastructure;

public class UIThread : UIThreadBase
{
    private readonly IDispatcher _dispatcher;

    // Windows blocks foreground-stealing across processes. Without this
    // grant the restarted instance can launch and stay pinned in the
    // taskbar without coming to the front. ASFW_ANY (-1) lets *any*
    // subsequent process take the foreground from us.
    private const uint ASFW_ANY = unchecked((uint)-1);
    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);

    public UIThread(IDispatcher dispatcher) : base()
    {
        _dispatcher = dispatcher;
    }

    public override bool IsOn
    {
        get
        {
            return _dispatcher.IsDispatchRequired;
        }
    }

    public override void Yield()
    {
        //Application.DoEvents();
    }

    public override void ExitApplication()
    {
        Process.GetCurrentProcess().Kill();
    }

    public override void RestartApplication()
    {
        // Pass --restart so the new instance can force-focus its main
        // window (see RestartFocusHelper.RequestFocusOnLaunchIfNeeded).
        ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess()?.MainModule?.FileName,
            Arguments = "--restart",
            UseShellExecute = false,
        };

        try { AllowSetForegroundWindow(ASFW_ANY); } catch { }

        Process started = Process.Start(processStartInfo)!;

        // Grant the specific new process foreground rights too (belt-and-
        // suspenders — AllowSetForegroundWindow(ASFW_ANY) covers it, but
        // the per-PID grant survives elevation-token differences).
        try { AllowSetForegroundWindow((uint)started.Id); } catch { }

        Process.GetCurrentProcess().Kill();
    }
}
