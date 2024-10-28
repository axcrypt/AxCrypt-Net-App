using AxCrypt.Core.Runtime;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

namespace AxCrypt.App.Windows.Infrastructure;

public class UIThread : UIThreadBase
{
    private readonly App _app;
    public UIThread(App app) : base()
    {
        _app = app;
    }

    public override bool IsOn
    {
        get
        {
            return SynchronizationContext.Current != null;
        }
    }

    public override void Yield()
    {
        //Application.DoEvents();
    }

    public override void ExitApplication()
    {
        _app.Quit();
        //Application.Current?.Quit();
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
        _app.Quit();
        //Process.GetCurrentProcess().Kill();
    }
}
