namespace AxCrypt.App.Shared.Services;

public class FileDropService
{
    public event Action? OnFilesDraggedOver;
    public event Action? OnFilesDragLeave;
    public event Action<IList<string>>? OnFilesDropped;
    public event Action<IList<string>>? OnFoldersDropped;

    public string? CurrentPage { get; set; } = "";

    public void NotifyFilesDropped(IList<string> paths)
    {
        OnFilesDropped?.Invoke(paths);
    }

    public void NotifyFoldersDropped(IList<string> paths)
    {
        OnFoldersDropped?.Invoke(paths);
    }

    public void NotifyFilesDraggedOver()
    {
        OnFilesDraggedOver?.Invoke();
    }

    public void NotifyFilesDragLeave()
    {
        OnFilesDragLeave?.Invoke();
    }

    public void ResetOnDropped()
    {
        OnFilesDropped = null;
        OnFoldersDropped = null;
        OnFoldersDropped = null;
    }

}
