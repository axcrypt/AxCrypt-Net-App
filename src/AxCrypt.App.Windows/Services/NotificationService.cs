using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Desktop.Services.Interface;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services.Interface;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.UI.Windowing;
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

        notifier.Show(notification);
    }

    private void ToastActivated(ToastNotification sender, object args)
    {
        try
        {
            App.Current?.Dispatcher?.Dispatch(() =>
            {
                Microsoft.UI.Xaml.Window? window = Application.Current.Windows[0].Handler.PlatformView as Microsoft.UI.Xaml.Window;
                window.AppWindow.Show(true);
                window.AppWindow.SetPresenter(AppWindowPresenterKind.Default);
                Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter = ((Microsoft.UI.Windowing.OverlappedPresenter)window.AppWindow.Presenter);
                overlappedPresenter.Restore(true);

                AxCServiceProviderExtension.GetService<ITrayService>()?.Dispose();
            });
        }
        catch (Exception ex)
        {
            New<IReport>().Exception(ex);
        }
    }
}