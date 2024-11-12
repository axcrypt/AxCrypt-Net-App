using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Desktop;

public class ExplorerRefresh : IFileExplorer
{
    public void Notify()
    {
        NativeMethods.SHChangeNotify(NativeMethods.HChangeNotifyEventID.SHCNE_ALLEVENTS, NativeMethods.HChangeNotifyFlags.SHCNF_DWORD, IntPtr.Zero, IntPtr.Zero);
        NativeMethods.SHChangeNotify(NativeMethods.HChangeNotifyEventID.SHCNE_UPDATEDIR, NativeMethods.HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
    }
}