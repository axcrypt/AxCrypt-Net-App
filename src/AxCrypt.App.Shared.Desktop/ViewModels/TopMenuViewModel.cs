using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class TopMenuViewModel : ViewModelBase
{
    private MainViewModel? _mainViewModel;

    public TopMenuViewModel(UserNotificationService notificationService)
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _mainViewModel = AxCServiceProviderExtension.LogOnViewModel!.MainViewModel;
    }

    public void Initialize()
    {
        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await SetSoftwareStatus(); await DisplayUpdateCheckPopups(); });
    }

    public bool IsWideScreen { get; set; }

    public bool IsLargeScreen { get; set; }

    public string? SelectedLanguage { get; set; } = "en";

    public string SelectedLanguageImageUrl { get; set; } = "images/flag/FrmEng.svg";

    public string SelectedLanguageDisplayName { get; set; } = Texts.EnglishLanguageToolStripMenuItemText;

    public LogOnViewModel LogOnViewModel { get; set; }

    public string? VersionHoverText { get; set; }

    public bool ShowUpdate { get; set; }

    public void SetLanguageAsync(string cultureName)
    {
        Resolve.UserSettings.CultureName = cultureName;
        SetCulture();
    }

    private void SetCulture()
    {
        CultureInfo cultureInfo = new CultureInfo(Resolve.UserSettings.CultureName);
        Thread.CurrentThread.CurrentUICulture = cultureInfo;
        Content.Resource.Culture = cultureInfo;
    }

    public async Task CheckAxCryptVersionAsync(EventArgs e)
    {
        switch (_mainViewModel?.VersionUpdateStatus)
        {
            case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
            case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
            case VersionUpdateStatus.IsUpToDate:
            case VersionUpdateStatus.Unknown:
                _userInitiatedUpdateCheckPending = true;
                await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
                break;

            case VersionUpdateStatus.NewerVersionIsAvailable:
                _userInitiatedUpdateCheckPending = true;
                await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
                break;

            default:
                break;
        }
    }

    public async Task SetSoftwareStatus()
    {
        VersionUpdateStatus status = _mainViewModel!.VersionUpdateStatus;

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
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel!.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }
}