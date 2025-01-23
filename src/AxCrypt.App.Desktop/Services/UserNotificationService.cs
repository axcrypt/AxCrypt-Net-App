using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.App.Desktop.ViewModels.Notification;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Core.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.Services;

public class UserNotificationService
{
    private IStatusAlertService? _statusAlertService;
    private ProcessIndicatorService _ProcessIndicatorService;
    private LogOnViewModel _logOnViewModel;
    public UserNotificationService(ProcessIndicatorService processIndicatorService, IStatusAlertService statusAlertService, LogOnViewModel logOnViewModel)
    {
        _ProcessIndicatorService = processIndicatorService;
        _statusAlertService = statusAlertService;
        _logOnViewModel = logOnViewModel;
        NotificationModel = new(logOnViewModel);
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
        string subscriptionLevel = _logOnViewModel.SubscriptionLevel.ToString();

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
            bool result = await NotificationApiHelper.DeleteNotificationAsync(id);
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
}
