using AxCrypt.Abstractions;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Shared.Models;

public class FileBackground : IProgressBackground
{
    private string _name;

    public async Task WorkAsync(string name, Func<IProgressContext, Task<FileOperationContext>> workAsync, Func<FileOperationContext, Task> completeAsync, IProgressContext progress)
    {
        _name = name;

        Busy = true;
        OnWorkStatusChanged();
        FileOperationContext status = new FileOperationContext(String.Empty, ErrorStatus.Unknown);
        try
        {
            status = await Task.Run(async () => await workAsync(progress));
        }
        catch (OperationCanceledException)
        {
            status = new FileOperationContext(String.Empty, ErrorStatus.Canceled);
        }
        await completeAsync(status);
        Busy = false;
        OnWorkStatusChanged();
    }

    public void WaitForIdle()
    {
        while (Busy)
        {
            Thread.Sleep(0);
        }
    }

    public event EventHandler WorkStatusChanged;

    protected virtual void OnWorkStatusChanged()
    {
        EventHandler handler = WorkStatusChanged;
        if (handler != null)
        {
            handler(this, new EventArgs());
        }
    }

    public bool Busy
    {
        get;
        set;
    }

    public override string ToString()
    {
        return _name;
    }
}