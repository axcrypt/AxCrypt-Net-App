using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using Microsoft.Win32;
using static AxCrypt.Desktop.NativeMethods;
using static AxCrypt.Abstractions.TypeResolve;
using System.Runtime.InteropServices;

namespace AxCrypt.Desktop
{
    public class WindowsDeviceLocking : IDisposable
    {
        public event Func<object, DeviceLockedEventArgs, Task> DeviceWasLockedAsync;

        private IDelayTimer _timer = New<IDelayTimer>();

        private bool? _wasScreenOff;

        public WindowsDeviceLocking()
        {
            SystemEvents.SessionEnding += async (sender, e) => await SystemEvents_SessionEnding(sender, e);
            SystemEvents.SessionSwitch += async (sender, e) => await SystemEvents_SessionSwitch(sender, e);
            SystemEvents.PowerModeChanged += async (sender, e) => await SystemEvents_PowerModeChanged(sender, e);

            _timer.SetInterval(TimeSpan.FromSeconds(2));
            _timer.Elapsed += async (sender, e) => await PollScreenSaverState(sender, e);
        }

        /// <summary>
        /// Starts this instance. Must be called on the main UI thread.
        /// </summary>
        public void Start(IntPtr handle)
        {
            RegisterForPowerNotifications(handle);
            _timer.Start();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "msg")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "w")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Param")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "l")]
        public async Task Message(int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg != WM_POWERBROADCAST)
            {
                return;
            }
            if (wParam.ToInt32() != PBT_POWERSETTINGCHANGE)
            {
                return;
            }

            POWERBROADCAST_SETTING ps = (POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(POWERBROADCAST_SETTING));
            if (ps.PowerSetting != GUID_MONITOR_POWER_ON && ps.PowerSetting != GUID_CONSOLE_DISPLAY_STATE)
            {
                return;
            }

            if (ps.DataLength != Marshal.SizeOf(typeof(Int32)))
            {
                return;
            }

            IntPtr pData = IntPtr.Add(lParam, Marshal.SizeOf(ps));

            Int32 iData = (Int32)Marshal.PtrToStructure(pData, typeof(Int32));
            await Notify(iData == 0);
        }

        private async Task Notify(bool monitorIsOff)
        {
            if (!DidScreenTurnOff(monitorIsOff))
            {
                return;
            }

            await OnDeviceWasLockedAsync(new DeviceLockedEventArgs(DeviceLockReason.Temporary));
        }

        private IntPtr _handleToPowerOnNotificationRegistration;

        private IntPtr _handleToMonitorStateNotificationRegistration;

        private void RegisterForPowerNotifications(IntPtr handle)
        {
            _handleToPowerOnNotificationRegistration = NativeMethods.RegisterPowerSettingNotification(handle, ref NativeMethods.GUID_MONITOR_POWER_ON, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
            _handleToMonitorStateNotificationRegistration = NativeMethods.RegisterPowerSettingNotification(handle, ref NativeMethods.GUID_CONSOLE_DISPLAY_STATE, NativeMethods.DEVICE_NOTIFY_WINDOW_HANDLE);
        }

        private async Task PollScreenSaverState(object sender, EventArgs e)
        {
            await PollScreenSaverState();
            _timer.Start();
        }

        private async Task PollScreenSaverState()
        {
            const int SPI_GETSCREENSAVERRUNNING = 114;
            bool screenSaverIsRunning = false;

            if (!NativeMethods.SystemParametersInfo(SPI_GETSCREENSAVERRUNNING, 0, ref screenSaverIsRunning, 0))
            {
                return;
            }

            if (!DidScreenTurnOff(screenSaverIsRunning))
            {
                return;
            }

            await OnDeviceWasLockedAsync(new DeviceLockedEventArgs(DeviceLockReason.Temporary));
        }

        private bool DidScreenTurnOff(bool isScreenOff)
        {
            if (!_wasScreenOff.HasValue)
            {
                _wasScreenOff = isScreenOff;
            }

            if (isScreenOff == _wasScreenOff)
            {
                return isScreenOff;
            }

            _wasScreenOff = isScreenOff;
            return isScreenOff;
        }

        protected virtual async Task OnDeviceWasLockedAsync(DeviceLockedEventArgs e)
        {
            if (DeviceWasLockedAsync != null)
            {
                Delegate[] eventHandlers = DeviceWasLockedAsync.GetInvocationList();

                foreach (Delegate handler in eventHandlers)
                {
                    Func<object, DeviceLockedEventArgs, Task> asyncHandler = (Func<object, DeviceLockedEventArgs, Task>)handler;
                    try
                    {
                        await asyncHandler(this, e);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error invoking event handler: {ex.Message}");
                    }
                }
            }
        }


        private async Task SystemEvents_PowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Suspend:
                    await OnDeviceWasLockedAsync(new DeviceLockedEventArgs(DeviceLockReason.Temporary));
                    break;

                default:
                    break;
            }
        }

        private async Task SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionSwitchReason.ConsoleDisconnect:
                case SessionSwitchReason.RemoteDisconnect:
                case SessionSwitchReason.SessionLock:
                    await OnDeviceWasLockedAsync(new DeviceLockedEventArgs(DeviceLockReason.Temporary));
                    break;

                default:
                    break;
            }
        }

        private async Task SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            switch (e.Reason)
            {
                case SessionEndReasons.Logoff:
                case SessionEndReasons.SystemShutdown:
                    e.Cancel = true;
                    await OnDeviceWasLockedAsync(new DeviceLockedEventArgs(DeviceLockReason.Permanent));
                    break;

                default:
                    break;
            }
        }

        private bool _disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }
        }

        private void DisposeInternal()
        {
            if (_disposed)
            {
                return;
            }

            SystemEvents.SessionEnding -= async (sender, e) => await SystemEvents_SessionEnding(sender, e);
            SystemEvents.SessionSwitch -= async (sender, e) => await SystemEvents_SessionSwitch(sender, e);
            SystemEvents.PowerModeChanged -= async (sender, e) => await SystemEvents_PowerModeChanged(sender, e);

            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            if (_handleToPowerOnNotificationRegistration != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(_handleToPowerOnNotificationRegistration);
                _handleToPowerOnNotificationRegistration = IntPtr.Zero;
            }

            if (_handleToMonitorStateNotificationRegistration != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(_handleToMonitorStateNotificationRegistration);
                _handleToMonitorStateNotificationRegistration = IntPtr.Zero;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~WindowsDeviceLocking()
        {
            Dispose(false);
        }
    }
}