using AxCrypt.Core.Runtime;
using System;
using System.Reflection;

namespace AxCrypt.App.Desktop.Implementation;

public class DesktopVersion : IVersion
{
    public Version Current
    {
        get
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}
