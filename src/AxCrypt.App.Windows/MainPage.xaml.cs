using AxCrypt.App.Components.Services.Interface;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Runtime;
using Microsoft.AspNetCore.Components;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows
{
    public partial class MainPage : ContentPage
    {
        private CommandLine _commandLine;

        HomeViewModel viewModel;
        public MainPage()
        {
            InitializeComponent();
            //new Styling(Resources.axcrypticon).Style(this, _recentFilesContextMenuStrip, _watchedFoldersContextMenuStrip);
        }

        public MainPage(CommandLine commandLine, NavigationManager navigationManager, HomeViewModel homeModel) : this()
        {
            viewModel = homeModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            New<IRuntimeEnvironment>().FirstInstanceIsReady();
            viewModel.AxCryptMainForm_ShownAsync();
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
