using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Components.Utility;

namespace AxCrypt.App.Components.Services;

public class StatusAlertService : IStatusAlertService
{
    public event Action<bool>? OnPopupVisibilityChanged;

    private bool _isVisible;

    private readonly int _milliSecondsDelay;

    public StatusAlertService()
    {
        _milliSecondsDelay = 10000;
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnPopupVisibilityChanged?.Invoke(_isVisible);
        }
    }

    public string? Alert { get; set; }

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

        DelayAutoHide(_milliSecondsDelay);
    }

    public void Hide()
    {
        Alert = "";
        IsVisible = false;
    }

    private async void DelayAutoHide(int milliSecondsDelay)
    {
        await Task.Delay(milliSecondsDelay);
        Hide();
    }
}
