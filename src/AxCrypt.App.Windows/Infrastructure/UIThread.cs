using AxCrypt.Core.Runtime;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AxCrypt.App.Windows.Infrastructure;

public class UIThread : UIThreadBase
{
    private readonly IDispatcher _dispatcher;
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
        _dispatcher.GetSynchronizationContextAsync().Dispose();
        //Process.GetCurrentProcess().Kill();
    }

    public override void RestartApplication()
    {
        ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = Process.GetCurrentProcess()?.MainModule?.FileName,
            UseShellExecute = false,
        };

        Process.Start(processStartInfo);
        _dispatcher.GetSynchronizationContextAsync().Dispose();
        //Process.GetCurrentProcess().Kill();
    }
}
