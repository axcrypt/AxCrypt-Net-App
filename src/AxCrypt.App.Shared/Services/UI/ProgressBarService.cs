namespace AxCrypt.App.Shared.Services;

public class ProgressBarService
{
    public ProgressBarService()
    {
        IsVisible = false;
    }

    public event Action<bool>? OnProgressBarVisibilityChanged;

    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnProgressBarVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public string? Filename { get; set; } = "";

    private double _progress = 0;

    public double Percentage
    {
        get
        {
            return _progress;
        }
        set
        {
            _progress = value;
            bool isVisible = (_progress == 100 || _progress == 0) ? _isVisible : true;
            isVisible = isVisible && !string.IsNullOrEmpty(Filename);
            IsVisible = isVisible;
        }
    }

    public void Hide()
    {
        Filename = "";
        Percentage = 0;
        IsVisible = false;
    }
}