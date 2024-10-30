using AxCrypt.Core.Runtime;
using System.Reflection;

namespace AxCrypt.App.Windows.Desktop;

internal class DesktopVersion : IVersion
{
    public Version Current
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}
