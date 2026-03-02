using AxCrypt.Content;

namespace AxCrypt.App.Shared.Services;

public class ProcessIndicatorService
{
    public event Action<bool>? OnProcessIndicatorVisibilityChanged;

    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnProcessIndicatorVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public string? Title { get; set; }

    public string? Message { get; set; }

    public bool FullScreen { get; set; }

    public void Show(string? title = null, string? message = null, bool isFullScreen = false)
    {
        Title = title ?? Texts.ProgressIndicatorWaitMessage;
        Message = message ?? Texts.ProgressIndicatorWaitMessage;
        IsVisible = true;
        FullScreen = isFullScreen;
    }

    public void Hide()
    {
        Title = "";
        Message = "";
        IsVisible = false;
    }
}