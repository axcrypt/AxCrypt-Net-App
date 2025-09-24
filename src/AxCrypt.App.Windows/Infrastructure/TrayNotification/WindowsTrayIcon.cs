using AxCrypt.App.Shared.Desktop.Code;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Windows.Infrastructure.TrayNotification;
using AxCrypt.Content;
using System.Diagnostics;

namespace AxCrypt.App.Windows.Infrastructure;

public class WindowsTrayIcon
{
    private readonly object lockObject = new object();


    private NotifyIconData iconData;

    /// <summary>
    /// Receives messages from the taskbar icon.
    /// </summary>
    private readonly WindowMessageSink messageSink;

    public Action<ContextMenuItem>? OnMenuItemClicked { get; set; }

    public bool IsTaskbarIconCreated { get; private set; }
    private bool _isTrayIconRegistered = false;

    public WindowsTrayIcon(string iconFile)
    {
        messageSink = new WindowMessageSink();

        // init icon data structure
        iconData = NotifyIconData.CreateDefault(messageSink.MessageWindowHandle, iconFile);
        iconData.ToolTipText = Texts.WixMsiDescription;
        // create the taskbar icon
        CreateTaskbarIcon();

        // register event listeners
        messageSink.MouseEventReceived += MessageSink_MouseEventReceived;
        messageSink.TaskbarCreated += MessageSink_TaskbarCreated;
    }

    private void MessageSink_TaskbarCreated()
    {
        DisposeTrayIcon();
        CreateTaskbarIcon();
    }

    private void MessageSink_MouseEventReceived(MouseEvent obj)
    {
        if (obj == MouseEvent.IconLeftMouseUp)
        {
            OnMenuItemClicked?.Invoke(ContextMenuItem.Advanced);
        }
        else if (obj == MouseEvent.IconRightMouseUp)
        {
            ShowTrayContextMenu();
        }
    }

    private void CreateTaskbarIcon()
    {
        lock (lockObject)
        {
            if (IsTaskbarIconCreated || _isTrayIconRegistered)
            {
                return;
            }

            const IconDataMembers members = IconDataMembers.Message
                                            | IconDataMembers.Icon
                                            | IconDataMembers.Tip;

            //write initial configuration
            var status = WriteIconData(ref iconData, NotifyCommand.Add, members);
            if (!status)
            {
                // couldn't create the icon - we can assume this is because explorer is not running (yet!)
                // -> try a bit later again rather than throwing an exception. Typically, if the windows
                // shell is being loaded later, this method is being re-invoked from OnTaskbarCreated
                // (we could also retry after a delay, but that's currently YAGNI)
                return;
            }

            //set to most recent version
            SetVersion();
            //messageSink.Version = (NotifyIconVersion)iconData.VersionOrTimeout;

            IsTaskbarIconCreated = true;
            _isTrayIconRegistered = true;
        }
    }

    private void RemoveTaskbarIcon()
    {
        lock (lockObject)
        {
            // make sure we didn't schedule a creation
            if (!IsTaskbarIconCreated)
            {
                return;
            }

            WriteIconData(ref iconData, NotifyCommand.Delete, IconDataMembers.Message);
            IsTaskbarIconCreated = false;
            _isTrayIconRegistered = false;
        }
    }

    public void EnsureVisible()
    {
        if (!IsTaskbarIconCreated || !_isTrayIconRegistered)
        {
            ShowTrayIcon();
        }
    }

    public void ShowTrayIcon()
    {
        CreateTaskbarIcon();
    }

    public void HideTrayIcon()
    {
        RemoveTaskbarIcon();
    }

    private void SetVersion()
    {
        iconData.VersionOrTimeout = (uint)0x4;
        bool status = WinApi.Shell_NotifyIcon(NotifyCommand.SetVersion, ref iconData);

        if (!status)
        {
            Debug.Fail("Could not set version");
        }
    }

    public static readonly object SyncRoot = new object();


    /// <summary>
    /// Updates the taskbar icons with data provided by a given
    /// <see cref="NotifyIconData"/> instance.
    /// </summary>
    /// <param name="data">Configuration settings for the NotifyIcon.</param>
    /// <param name="command">Operation on the icon (e.g. delete the icon).</param>
    /// <returns>True if the data was successfully written.</returns>
    /// <remarks>See Shell_NotifyIcon documentation on MSDN for details.</remarks>
    public static bool WriteIconData(ref NotifyIconData data, NotifyCommand command)
    {
        return WriteIconData(ref data, command, data.ValidMembers);
    }


    /// <summary>
    /// Updates the taskbar icons with data provided by a given
    /// <see cref="NotifyIconData"/> instance.
    /// </summary>
    /// <param name="data">Configuration settings for the NotifyIcon.</param>
    /// <param name="command">Operation on the icon (e.g. delete the icon).</param>
    /// <param name="flags">Defines which members of the <paramref name="data"/>
    /// structure are set.</param>
    /// <returns>True if the data was successfully written.</returns>
    /// <remarks>See Shell_NotifyIcon documentation on MSDN for details.</remarks>
    public static bool WriteIconData(ref NotifyIconData data, NotifyCommand command, IconDataMembers flags)
    {

        data.ValidMembers = flags;
        lock (SyncRoot)
        {
            return WinApi.Shell_NotifyIcon(command, ref data);
        }
    }

    private void ShowTrayContextMenu()
    {
        IntPtr hMenu = WinApiContext.CreatePopupMenu();

        bool isLoggedOn = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel.LoggedOn;

        uint uId = 1;
        WinApiContext.AppendMenu(hMenu, 0, (uint)uId, Texts.McInfMenuShow);
        if (isLoggedOn)
        {
            uId++;
            WinApiContext.AppendMenu(hMenu, 0, (uint)uId, Texts.PromptSignOut);
        }
        uId++;
        WinApiContext.AppendMenu(hMenu, 0, (uint)uId, Texts.ButtonExitText);

        // Get current cursor position
        WinApiContext.GetCursorPos(out WinApiContext.POINT pt);
        pt.Y -= 2;

        if (iconData.WindowHandle != IntPtr.Zero && WinApiContext.IsWindow(iconData.WindowHandle))
        {
            WinApiContext.SetForegroundWindow(iconData.WindowHandle);
        }

        int cmd = WinApiContext.TrackPopupMenu(hMenu, WinApiContext.TPM_LEFTALIGN | WinApiContext.TPM_BOTTOMALIGN | WinApiContext.TPM_RETURNCMD, pt.X, pt.Y, 0, iconData.WindowHandle, IntPtr.Zero);
        switch (cmd)
        {
            case 1:
                OnMenuItemClicked?.Invoke(ContextMenuItem.Advanced);
                break;
            case 2 when isLoggedOn:
                OnMenuItemClicked?.Invoke(ContextMenuItem.SignOut);
                break;
            case 2 when !isLoggedOn:
            case 3:
                OnMenuItemClicked?.Invoke(ContextMenuItem.Exit);
                break;

            default:
                OnMenuItemClicked?.Invoke(ContextMenuItem.None);
                break;
        }
    }

    public void DisposeTrayIcon()
    {
        RemoveTaskbarIcon();
        messageSink?.Dispose();
    }
}
