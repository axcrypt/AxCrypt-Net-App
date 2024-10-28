using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Services;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            //New<IRuntimeEnvironment>().FirstInstanceIsReady();

        }

        protected override void ChangeVisualState()
        {
            base.ChangeVisualState();
        }

        protected override void OnDisappearing()
        {
            SetupTrayIcon();
            base.OnDisappearing();
        }

        private void SetupTrayIcon()
        {
            ITrayService trayService = new TrayService();
            if (trayService != null)
            {
                trayService.Initialize();

                INotificationService notificationService = new NotificationService();
                trayService.ClickHandler = () =>
                    notificationService
                        ?.ShowNotification("AxCrypt File Encryption", "Click here to restore the window");
            }
        }
    }
}
