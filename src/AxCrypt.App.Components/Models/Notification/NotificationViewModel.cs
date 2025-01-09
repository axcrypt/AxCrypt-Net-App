using AxCrypt.App.Components.Utility.View;
using System.Collections.ObjectModel;

namespace AxCrypt.App.Components.Models.Notification
{
    public class NotificationViewModel : Core.UI.ViewModel.ViewModelBase
    {
        private readonly LogOnViewModel? _logOnViewModel;
        public NotificationViewModel(LogOnViewModel logOnViewModel)
        {
            Notifications = new ObservableCollection<NotificationItemViewModel>();
            _logOnViewModel = logOnViewModel;
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
                _logOnViewModel?.UIStateChanged();
            }
        }

        public SortDirection DateSortDirection { get; set; } = SortDirection.Ascending;

    }
}