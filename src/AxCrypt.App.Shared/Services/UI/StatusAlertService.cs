using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;

namespace AxCrypt.App.Shared.Services;

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

    public AlertNotificationType Type { get; set; }

    public void Success(string alert)
    {
        Type = AlertNotificationType.Success;
        Show(alert);
    }

    public void Error(string alert)
    {
        Type = AlertNotificationType.Warning;
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