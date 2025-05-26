namespace AxCrypt.App.Shared.Services;

public class FileDropService
{
    public event Action<List<string>>? OnFilesDropped;
    public event Action<List<string>>? OnFoldersDropped;

    public string? CurrentPage { get; set; } = "";

    public void NotifyFilesDropped(List<string> paths)
    {
        OnFilesDropped?.Invoke(paths);
    }

    public void NotifyFoldersDropped(List<string> paths)
    {
        OnFoldersDropped?.Invoke(paths);
    }
}
