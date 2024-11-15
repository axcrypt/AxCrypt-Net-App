using AxCrypt.App.Components.Utility.View;
using System.Collections.ObjectModel;

namespace AxCrypt.App.Components.Models.Notification
{
    public class NotificationViewModel : Core.UI.ViewModel.ViewModelBase
    {
        private readonly Action? _stateChangeCallback;

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

        public string? ErrorMessage { get; set; }

        public SortDirection DateSortDirection { get; set; } = SortDirection.Ascending;

    }
}