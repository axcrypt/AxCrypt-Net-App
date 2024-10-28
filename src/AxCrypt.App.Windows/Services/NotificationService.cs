
using AxCrypt.App.Components.Services.Interface;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AxCrypt.App.Windows.Services;

public class NotificationService : INotificationService
{
    public void ShowNotification(string title, string body)
    {
        new ToastContentBuilder()
            .AddToastActivationInfo(null, ToastActivationType.Foreground)
            //.AddAppLogoOverride(new Uri("ms-appx:///Assets/dotnet_bot.svg"))
            .AddText(title, hintStyle: AdaptiveTextStyle.Header)
            .AddText(body, hintStyle: AdaptiveTextStyle.Body)
            .SetToastDuration(ToastDuration.Short)
            .Show();
    }
}
