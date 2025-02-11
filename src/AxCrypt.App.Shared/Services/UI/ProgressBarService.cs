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
            if (_isVisible != value)
            {
                _isVisible = value;
                OnProgressBarVisibilityChanged?.Invoke(_isVisible);
            }
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
            if (_progress != value)
            {
                _progress = value;
                bool isVisible = (_progress == 100 || _progress == 0) ? _isVisible : true;
                OnProgressBarVisibilityChanged?.Invoke(isVisible);
            }
        }
    }

    public void Show()
    {
        Filename = "";
        IsVisible = true;
    }

    //public void UpdateOnProgress(string fileName, double progress, bool isVisible)
    //{
    //    Filename = fileName;
    //    Percentage = progress;
    //    OnProgressBarVisibilityChanged?.Invoke(isVisible);
    //}

    public void Hide()
    {
        Filename = "";
        Percentage = 0;
        IsVisible = false;
    }
}