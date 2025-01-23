using AxCrypt.App.Desktop.Models;
using AxCrypt.App.Desktop.Services;
using AxCrypt.App.Windows.Models;
using AxCrypt.App.Windows.Services;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Diagnostics;
using System.Globalization;

namespace AxCrypt.App.Windows.ViewModels;

public class TopMenuViewModel : ViewModelBase
{
    private readonly UserNotificationService _notificationService;
    private MainViewModel? _mainViewModel;

    public TopMenuViewModel(UserNotificationService notificationService)
    {
        LogOnViewModel = AxCServiceProvider.LogOnViewModel!; 
        _notificationService = notificationService;
        _mainViewModel = AxCServiceProvider.LogOnViewModel!.MainViewModel;
        TopMenuModel = new TopMenuModel();
        TopMenuModel.SubscriptionLevel = AxCServiceProvider.LogOnViewModel!.SubscriptionLevel;
    }

    public void Initialize()
    {
        TopMenuModel.UserEmail = Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address;
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await SetSoftwareStatus(); await DisplayUpdateCheckPopups(); });
    }

    public TopMenuModel TopMenuModel { get; set; }

    public LogOnViewModel LogOnViewModel { get; set; }

    public DeviceIdiom GetCurrentDeviceIdiom() => DeviceInfo.Idiom;

    public string VersionHoverText { get; set; }
    public bool ShowUpdate { get; set; }

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

    public async Task SetSoftwareStatus()
    {
        VersionUpdateStatus status = _mainViewModel.VersionUpdateStatus;

        switch (status)
        {
            case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
            case VersionUpdateStatus.IsUpToDate:
                ShowUpdate = false;
                break;

            case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
                VersionHoverText = Texts.OldVersionTooltip;
                break;

            case VersionUpdateStatus.NewerVersionIsAvailable:
                VersionHoverText = Texts.NewVersionIsAvailableText.InvariantFormat(_mainViewModel.DownloadVersion.Version) + ' ' + Texts.ClickToDownloadText;
                break;

            case VersionUpdateStatus.Unknown:
                VersionHoverText = Texts.ClickToCheckForNewerVersionTooltip;
                break;
        }
    }

    private bool _userInitiatedUpdateCheckPending = false;

    private async Task DisplayUpdateCheckPopups()
    {
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }
}