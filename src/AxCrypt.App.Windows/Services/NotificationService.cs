using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Notifications;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

public class NotificationService : INotificationService
{
    public void ShowNotification(string title, string message)
    {
        ToastContentBuilder? toast = new ToastContentBuilder()
            .AddArgument("action", "restorewindow")
            .AddText(title)
            .AddText(message);

        ToastNotifier? notifier = ToastNotificationManager.CreateToastNotifier();
        ToastNotification? notification = new ToastNotification(toast.GetXml());
        notification.Activated += ToastActivated;

        notification.Dismissed += ToastDismissed;
        notification.Failed += ToastFailed;

        notifier.Show(notification);
    }

    private void ToastActivated(ToastNotification sender, object args)
    {
        try
        {
            AxCServiceProvider.GetService<IWindowService>().RestoreWindowWithFocus();
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
        }
    }

    private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs args)
    {
        try
        {
            AxCServiceProvider.GetService<ITrayService>()?.EnsureVisible();
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
        }
    }

    private void ToastFailed(ToastNotification sender, ToastFailedEventArgs args)
    {
        try
        {
            AxCServiceProvider.GetService<ITrayService>()?.EnsureVisible();
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
        }
    }
}