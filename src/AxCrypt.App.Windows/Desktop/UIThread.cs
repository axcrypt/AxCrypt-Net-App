using AxCrypt.Core.Runtime;
using System.Diagnostics;

namespace AxCrypt.App.Windows.Desktop;

public class UIThread : UIThreadBase
{
    public override bool IsOn
    {
        get { return SynchronizationContext.Current != null; }
    }

    public override void Yield()
    {
    }

    public override void ExitApplication()
    {
        Process.GetCurrentProcess().Kill();
    }

    public override void RestartApplication()
    {
        ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName,
            UseShellExecute = false,
        };

        System.Diagnostics.Process.Start(processStartInfo);
        Process.GetCurrentProcess().Kill();
    }
}
