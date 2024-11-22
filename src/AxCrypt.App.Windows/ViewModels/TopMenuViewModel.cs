using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Windows.Models;
using AxCrypt.Common;
using AxCrypt.Core;
using AxCrypt.Core.UI.ViewModel;
using System.Diagnostics;
using System.Globalization;

namespace AxCrypt.App.Windows.ViewModels;

public class TopMenuViewModel
{
    private LogOnViewModel _logOnViewModel;
    private readonly UserNotificationService _notificationService;
    private MainViewModel? _mainViewModel;

    public TopMenuViewModel(UserNotificationService notificationService, LogOnViewModel logOnViewModel)
    {
        _logOnViewModel = logOnViewModel;
        _notificationService = notificationService;
        _mainViewModel = logOnViewModel.MainViewModel;
        TopMenuModel = new TopMenuModel();
        TopMenuModel.SubscriptionLevel = logOnViewModel.SubscriptionLevel;
    }

    public void Initialize()
    {
        TopMenuModel.UserEmail = Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address;
    }

    public TopMenuModel TopMenuModel { get; set; }

    public DeviceIdiom GetCurrentDeviceIdiom() => DeviceInfo.Idiom;

    public void ToggleAccountPopup() => TopMenuModel.AccountPopup = !TopMenuModel.AccountPopup;
    public void ToggleSettingsPopup() => TopMenuModel.SettingsPopup = !TopMenuModel.SettingsPopup;
    public void ToggleNotifyPopup() => TopMenuModel.NotifyPopup = !TopMenuModel.NotifyPopup;
    public void ToggleDropdown() => TopMenuModel.ShowDropdown = !TopMenuModel.ShowDropdown;

    public void SetLanguageAsync(string cultureName)
    {
        Resolve.UserSettings.CultureName = cultureName;
        SetCulture();
        InitializeContentResources();
    }

    private void SetCulture()
    {
        CultureInfo cultureInfo = new CultureInfo(Resolve.UserSettings.CultureName);
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Content.Resource.Culture = cultureInfo;
    }

    private void InitializeContentResources() => SetCulture();

    public async Task CheckAxCryptVersionAsync(EventArgs e)
    {
        switch (_mainViewModel?.VersionUpdateStatus)
        {
            case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
                await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
                break;
            case VersionUpdateStatus.NewerVersionIsAvailable:
                Process.Start(Resolve.UserSettings.UpdateUrl.ToString());
                break;
        }
    }

    public void CloseAccountPopup() => TopMenuModel.AccountPopup = false;
    public void CloseSettingsPopup() => TopMenuModel.SettingsPopup = false;
}