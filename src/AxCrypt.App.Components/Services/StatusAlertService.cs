using AxCrypt.App.Components.Services.Interface;

namespace AxCrypt.App.Components.Services;

public class StatusAlertService : IStatusAlertService
{
    public event Action<bool> OnPopupVisibilityChanged;

    private bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPopupVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public string Alert { get; set; }

    public NotificationType Type { get; set; }

    public void Success(string alert)
    {
        Type = NotificationType.Success;
        Show(alert);
    }
    
    public void Error(string alert)
    {
        Type = NotificationType.Warning;
        Show(alert);
    }

    private void Show(string alert)
    {
        Alert = alert;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }
}
