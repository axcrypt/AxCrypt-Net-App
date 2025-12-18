using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.ViewModels.Notification;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.Core.Notification;
using AxCrypt.Common;

namespace AxCrypt.App.Shared.Services;

public class UserNotificationService : ViewModelBase
{
    private IStatusAlertService? _statusAlertService;
    private LogOnViewModel _logOnViewModel;
    public UserNotificationService(IStatusAlertService statusAlertService, LogOnViewModel logOnViewModel)
    {
        _statusAlertService = statusAlertService;
        _logOnViewModel = logOnViewModel;
        NotificationModel = new();
    }

    private NotificationViewModel? _notificationModel;
    public NotificationViewModel NotificationModel
    {
        get
        {
            return _notificationModel!;
        }
        set
        {
            _notificationModel = value;
        }
    }

    public async Task LoadNotificationListAsync()
    {
        if (!New<AxCryptOnlineState>().IsOnline)
        {
            NotificationModel = new NotificationViewModel();
            return;
        }

        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            IEnumerable<NotificationItemViewModel> notificationItems = await LoadNotificationsAsync();
            NotificationModel.Notifications = new ObservableCollection<NotificationItemViewModel>(notificationItems);
            UpdateViewState();
        }
    }

    private async Task<IEnumerable<NotificationItemViewModel>> LoadNotificationsAsync()
    {
        AxCrypt.Core.Crypto.LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        string subscriptionLevel = _logOnViewModel.SubscriptionLevel.ToString();

        IEnumerable<Api.Model.Notification.UserNotificationApiModel> notifications = await New<IUserNotificationService>().GetNotificationAsync(identity.UserEmail.Address, subscriptionLevel);
        return notifications.Select(nf => new NotificationItemViewModel(nf));
    }

    public async Task<bool> DeleteNotificationAsync(long id)
    {
        if (id == 0)
        {
            return false;
        }
        using (ProcessIndicator processIndicator = new ProcessIndicator())
        {
            bool result = await New<IUserNotificationService>().DeleteNotificationAsync(id);
            await LoadNotificationListAsync();

            if (result)
            {
                _statusAlertService?.Success("Your notification is deleted successfully!");
            }
            else
            {
                _statusAlertService?.Error("Failed to delete the notification. Please check your internet connection and try again.");
            }

            return result;
        }
    }

    public async Task SortNotificationsByDate(SortDirection dateSortDirection)
    {
        if (!NotificationModel.Notifications.Any())
        {
            await LoadNotificationListAsync();
        }

        IEnumerable<NotificationItemViewModel> sortedNotifications = new List<NotificationItemViewModel>();
        if (dateSortDirection == SortDirection.Ascending)
        {
            sortedNotifications = NotificationModel.Notifications.OrderBy(n => n.CreatedDate);
        }

        if (dateSortDirection == SortDirection.Descending)
        {
            sortedNotifications = NotificationModel.Notifications.OrderByDescending(n => n.CreatedDate);
        }

        NotificationModel.Notifications = new ObservableCollection<NotificationItemViewModel>(sortedNotifications);
    }

    public void HandleNotificationAction(string eventType)
    {
        if (string.IsNullOrEmpty(eventType))
        {
            return;
        }

        if (!Enum.TryParse(eventType, out NotificationType notificationActionEventType))
        {
            return;
        }

        switch (notificationActionEventType)
        {
            case NotificationType.GetStarted:
                OpenWebPage("https://axcrypt.net/information/guides/getstarted/");
                break;
            default:
                break;
        }
    }

    private void OpenWebPage(string url)
    {
        AxCrypt.Core.BrowseUtility.RedirectTo(url);
    }
}
