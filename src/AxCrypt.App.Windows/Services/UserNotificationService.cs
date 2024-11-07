using AxCrypt.App.Components.Models;
using AxCrypt.App.Windows.Helpers;
using AxCrypt.App.Windows.Models;
using AxCrypt.Core.UI;
using System.Collections.ObjectModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Services;

public class UserNotificationService
{
    public UserNotificationService()
    {
        NotificationModel = new();
    }

    public NotificationViewModel NotificationModel { get; set; }

    public async Task LoadNotificationListAsync(IProgress<LoadingModel> progress = null)
    {
        await Services.LoadingProgressHelper.ExecuteLoadingProgress<Task>(async () =>
        {
            IEnumerable<NotificationItemViewModel> notificationItems = await LoadNotificationsAsync();
            NotificationModel.Notifications = new ObservableCollection<NotificationItemViewModel>(notificationItems);

            return Task.CompletedTask;
        }, progress);
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

        return await NotificationApiHelper.DeleteNotificationAsync(id);
    }

    public async Task SortNotificationsByDate(SortDirection dateSortDirection, IProgress<LoadingModel> progress)
    {
        if (!NotificationModel.Notifications.Any())
        {
            await LoadNotificationListAsync(progress);
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
