using AxCrypt.Core.UI;
using AxCrypt.Desktop;

namespace AxCrypt.App.Windows.Infrastructure;

public class DeviceLocked : IDeviceLocked, IDisposable
{
    private WindowsDeviceLocking _deviceLocking = new WindowsDeviceLocking();

    public event Func<object, DeviceLockedEventArgs, Task> DeviceWasLockedAsync;

    public DeviceLocked()
    {
        _deviceLocking.DeviceWasLockedAsync += async (sender, e) => await DeviceLocked_DeviceWasLocked(sender, e);
    }

    private async Task DeviceLocked_DeviceWasLocked(object sender, DeviceLockedEventArgs e)
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
                    // Handle exception (optional)
                    Console.WriteLine($"Error invoking event handler: {ex.Message}");
                }
            }
        }
    }


    /// <summary>
    /// Starts this instance. Must be called on the main UI thread.
    /// </summary>
    public void Start(object state)
    {
        _deviceLocking.Start(0);
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

        if (_deviceLocking != null)
        {
            _deviceLocking.DeviceWasLockedAsync -= async (sender, e) => await DeviceLocked_DeviceWasLocked(sender, e);
            _deviceLocking.Dispose();
            _deviceLocking = null;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~DeviceLocked()
    {
        Dispose(false);
    }
}