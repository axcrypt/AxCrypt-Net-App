using AxCrypt.App.Components.Data;
using AxCrypt.App.Components.Models.Notification;
using AxCrypt.App.Components.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Windows.ViewModels
{
    // TopMenuViewModel.cs
    public class TopMenuViewModel : ComponentBase
    {
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly UserNotificationService _notificationService;

        public TopMenuViewModel(NavigationManager navigationManager, IJSRuntime jsRuntime, UserNotificationService notificationService)
        {
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
            _notificationService = notificationService;
        }

        public NotificationViewModel NotificationModel { get; private set; } = new NotificationViewModel();
        public bool IsWideScreen { get; private set; }
        public bool IsLargeScreen { get; private set; }
        public bool IsLoading { get; private set; } = true;
        public bool AccountPopup { get; private set; }
        public bool SettingsPopup { get; private set; }
        public bool NotifyPopup { get; private set; }
        public bool ShowDropdown { get; private set; }
        public string SelectedLanguageImageUrl { get; private set; } = "images/flag/FrmEng.svg";
        public string SelectedLanguageDisplayName { get; private set; } = "Eng(US)";
        public string SelectedLanguage { get; private set; }
        public string UserEmail { get; private set; } = "you";

        public async Task InitializeAsync(AppLocalizationOptions localizationOptions, string currentCulture)
        {
            // Setup language selection
            CultureOption? cultureOption = localizationOptions.SupportedCultures.FirstOrDefault(c => c.Name == currentCulture);
            if (cultureOption != null)
            {
                SelectedLanguage = cultureOption.Name;
                SelectedLanguageImageUrl = cultureOption.ImageUrl;
                SelectedLanguageDisplayName = cultureOption.DisplayName;
            }

            //NotificationModel = await _notificationService.LoadNotificationListAsync();

            // Check screen size
            IsWideScreen = await Utility.IsWideScreenAsync(_jsRuntime);
            IsLargeScreen = await Utility.IsLargeScreenAsync(_jsRuntime);
            IsLoading = false;
        }

        public void ToggleAccountPopup() => AccountPopup = !AccountPopup;
        public void ToggleSettingsPopup() => SettingsPopup = !SettingsPopup;
        public void ToggleNotifyPopup() => NotifyPopup = !NotifyPopup;

        public async Task ChangeLanguageAsync(string cultureName, string imageUrl)
        {
            SelectedLanguage = cultureName;
            SelectedLanguageImageUrl = imageUrl;
            //SelectedLanguageDisplayName = appLocalizationOptions?.SupportedCultures.FirstOrDefault(c => c.Name == cultureName)?.DisplayName;
            await SetLanguageAsync(cultureName);
            ShowDropdown = false;
            _navigationManager.NavigateTo(_navigationManager.Uri, forceLoad: true);
        }

        private async Task SetLanguageAsync(string cultureName)
        {
            // Implementation for setting culture, language, etc.
        }
    }

}
