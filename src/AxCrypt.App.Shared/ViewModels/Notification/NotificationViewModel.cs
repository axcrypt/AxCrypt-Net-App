using AxCrypt.App.Shared.Models.Notification;
using AxCrypt.App.Shared.Utility.View;
using System.Collections.ObjectModel;

namespace AxCrypt.App.Shared.ViewModels.Notification
{
    public class NotificationViewModel : Core.UI.ViewModel.ViewModelBase
    {
        public NotificationViewModel()
        {
            Notifications = new ObservableCollection<NotificationItemViewModel>();
        }

        public ObservableCollection<NotificationItemViewModel> Notifications
        {
            get
            {
                return GetProperty<ObservableCollection<NotificationItemViewModel>>(nameof(Notifications));
            }
            set
            {
                SetProperty(nameof(Notifications), value);
            }
        }

        public SortDirection DateSortDirection { get; set; } = SortDirection.Ascending;

    }
}