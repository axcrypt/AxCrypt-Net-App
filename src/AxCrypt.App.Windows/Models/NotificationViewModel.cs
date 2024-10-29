using AxCrypt.App.Windows.Helpers;
using System.Collections.ObjectModel;

namespace AxCrypt.App.Windows.Models
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

        private bool _loading { get; set; } = true;

        private Action? _onStateChange;

        public void SetOnStateChange(Action onStateChange)
        {
            _onStateChange = onStateChange;
        }

        public bool Loading
        {
            get => _loading;
            set
            {
                if (_loading != value)
                {
                    _loading = value;
                    _onStateChange?.Invoke();
                }
            }
        }

        public string? ErrorMessage { get; set; }

        public SortDirection DateSortDirection { get; set; } = SortDirection.Ascending;

    }
}
