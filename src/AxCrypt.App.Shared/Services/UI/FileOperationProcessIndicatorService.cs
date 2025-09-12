using AxCrypt.Content;

namespace AxCrypt.App.Shared.Services;

public class FileOperationProcessIndicatorService
{
    public event Action<bool>? OnFileOperationProcessIndicatorVisibilityChanged;

    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnFileOperationProcessIndicatorVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public void Show(string? title = null, string? message = null)
    {
        Title = title ?? Texts.ProgressIndicatorWaitMessage;
        Message = message ?? Texts.ProgressIndicatorWaitMessage;
        IsVisible = true;
    }

    public void Hide()
    {
        Title = "";
        Message = "";
        IsVisible = false;
    }
}