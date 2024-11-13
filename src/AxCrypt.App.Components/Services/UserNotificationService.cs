using AxCrypt.App.Components.Helpers;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Models.Notification;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.Core.UI;
using System.Collections.ObjectModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Services;

public class UserNotificationService
{
    private ProcessIndicatorService _ProcessIndicatorService;

    public UserNotificationService(ProcessIndicatorService processIndicatorService)
    {
        _ProcessIndicatorService = processIndicatorService;
        NotificationModel = new();
    }

    public NotificationViewModel NotificationModel { get; set; }

    public async Task LoadNotificationListAsync()
    {
        using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
        {
            IEnumerable<NotificationItemViewModel> notificationItems = await LoadNotificationsAsync();
            NotificationModel.Notifications = new ObservableCollection<NotificationItemViewModel>(notificationItems);
        }
    }

    private async Task<IEnumerable<NotificationItemViewModel>> LoadNotificationsAsync()
    {
        AxCrypt.Core.Crypto.LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
        string subscriptionLevel = New<AccountStatusViewModel>().SubscriptionLevel.ToString();

        IEnumerable<Api.Model.Notification.UserNotificationApiModel> notifications = await NotificationApiHelper.GetNotificationAsync(identity.UserEmail.Address, subscriptionLevel);
        return notifications.Select(nf => new NotificationItemViewModel(nf));
    }

    public async Task<bool> DeleteSecretAsync(long id)
    {
        if (id == 0)
        {
            return false;
        }
        using (ProcessIndicator processIndicator = new ProcessIndicator(_ProcessIndicatorService))
        {
            return await NotificationApiHelper.DeleteNotificationAsync(id);
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
}
