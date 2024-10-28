
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Services;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Infrastructure;

public class NotifyIconGlobalNotification : IGlobalNotification
{
    public NotifyIconGlobalNotification()
    {
        
    }

    public void ShowTransient(string title, string text)
    {
        INotificationService notificationService = new NotificationService();
        notificationService?.ShowNotification(title, text);
    }
}
