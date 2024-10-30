namespace AxCrypt.App.Components.Services
{
    public class AlertNotification : Core.UI.ViewModel.ViewModelBase
    {
        public string NotificationMessage { get; private set; }
        public bool IsNotificationVisible { get; private set; }
        public NotificationType Type { get; private set; }

        public event Action OnNotificationChanged;

        public void ShowNotification(string message, NotificationType type)
        {
            NotificationMessage = message;
            Type = type;
            IsNotificationVisible = true;
            NotifyStateChanged();
            StartAutoHideTimer();
        }

        public string NotificationClass => Type switch
        {
            NotificationType.Success => "success",
            NotificationType.Warning => "alert",
            _ => "alert"
        };

        public void HideNotification()
        {
            NotificationMessage = string.Empty;
            IsNotificationVisible = false;
            NotifyStateChanged();
        }

        private void NotifyStateChanged() => OnNotificationChanged?.Invoke();

        private async void StartAutoHideTimer()
        {
            await Task.Delay(20000);
            HideNotification();
        }
    }

    public enum NotificationType
    {
        Success,
        Warning,
    }
}