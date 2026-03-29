using AxCrypt.Abstractions;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Models;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.Interface;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Main;

public class AppSettingsViewModel : ViewModelBase
{
    private LogOnViewModel _logOnViewModel;
    private MainViewModel? _mainViewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private ManageAccountViewModel? _viewModel;
    private RecentFilesViewModel? _recentFilesViewModel;
    private LogViewModel? _logViewModel;

    public AppSettingsViewModel(RecentFilesViewModel recentFilesViewModel)
    {
        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _mainViewModel = _logOnViewModel.MainViewModel;
        _fileOperationViewModel = _logOnViewModel.FileOperationViewModel;
        _recentFilesViewModel = recentFilesViewModel;
        AlwaysOffline = New<UserSettings>().OfflineMode;
        HideRecentFiles = New<UserSettings>().HideRecentFiles;
        InactivitySignOut = New<UserSettings>().InactivitySignOutTime.Minutes;
        IncludeSubfolders = New<UserSettings>().FolderOperationMode == FolderOperationMode.IncludeSubfolders;
        IsFileNameOn = New<UserSettings>().EncryptFilePropertiesFileName;
        IsDateModifiedOn = New<UserSettings>().EncryptFilePropertiesDateModified;
        IsFileNameOn = New<UserSettings>().EncryptFilePropertiesFileName;
        AdvancedOptionsViewModel = new AdvancedOptionsViewModel();
        _logViewModel = AxCServiceProviderExtension.LogViewModel;
        EnableUserActivity = New<UserSettings>().UserActivityMode;
        EnableFindFiles = New<UserSettings>().FindFileMode;
        DarkTheme = New<UserSettings>().DarkMode;
    }

    public void Initialized()
    {
        RestApiBaseUrlInput = Resolve.UserSettings.RestApiBaseUrl.ToString();
        TimeoutInput = Resolve.UserSettings.ApiTimeout.ToString();

        _mainViewModel!.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { ConfigureMenusAccordingToPolicy(license); });
        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), (bool isLoggedOn) => { if (isLoggedOn) { StartInactivitySignOut(); } });
        _mainViewModel!.BindPropertyChanged(nameof(_mainViewModel.FolderOperationMode), (FolderOperationMode SecureFolderLevel) => { IncludeSubfolders = SecureFolderLevel == FolderOperationMode.IncludeSubfolders ? true : false; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptionUpgradeMode), (EncryptionUpgradeMode mode) => AutoUpgradeToAES256 = mode == EncryptionUpgradeMode.AutoUpgrade);
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DebugMode), (bool enabled) => { UpdateDebugMode(enabled); });
    }

    public AdvancedOptionsViewModel AdvancedOptionsViewModel { get; set; }

    public IEnumerable<int> InactivityTimeoutOptions { get; } = new List<int> { 0, 5, 15, 30, 60 };

    public bool FileEncryptionProperties { get; set; }
    public bool IsDateModifiedOn { get; set; }
    public bool IsFileNameOn { get; set; }
    public bool IncludeSubfolders { get; set; }
    public bool AlwaysOffline { get; set; }
    public bool AutoUpgradeToAES256 { get; set; }
    public bool InactSgnOut { get; set; }
    public Uri? DownloadVersion { get; set; }
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool ShowVersion { get; set; }
    public bool ShowOptions { get; set; }
    public bool ShowManageAxCryptID { get; set; }
    public DateTime CreatedTime { get; set; }
    public double SelectedOption { get; set; } = 0;
    public bool DebugPopup { get; set; }
    public bool EnableDebugPopup { get; set; }

    public bool EnableIncludeSubfolders { get; set; }

    public bool EnableInActivitySignOut { get; set; }

    public bool EnableRestoreRename { get; set; }

    public bool EnableAutoUpgrade { get; set; }

    public bool EnableEncryptionFileProperties { get; set; }

    public bool EnableInviteFriend { get; set; }

    public bool EnableFindFilesMenuOption { get; set; }

    public bool EnableVaultMenuOption { get; set; }

    private static bool _hideRecentFiles;

    public bool HideRecentFiles
    {
        get => _hideRecentFiles;
        set
        {
            _hideRecentFiles = value;
            _logOnViewModel.UIStateChanged();
        }
    }

    public int InactivitySignOut { get; set; }

    public string? SelectedLanguage { get; set; } = "en";

    public string SelectedLanguageImageUrl { get; set; } = "images/flag/FrmEng.svg";

    public string SelectedLanguageDisplayName { get; set; } = Texts.EnglishLanguageToolStripMenuItemText;

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

    public bool DarkTheme { get; set; }

    public void ToggleHideRecentFiles() => SetRecentFilesHiddenState(!New<UserSettings>().HideRecentFiles);

    public IDictionary<string, object> NavLinkAttributes1()
    {
        Dictionary<string, object> attributes = new Dictionary<string, object>();
        attributes["class"] = "nav-link" + (InactSgnOut ? " active" : "");
        return attributes;
    }

    public IDictionary<string, object> NavLinkAttributes2()
    {
        Dictionary<string, object> attributes = new Dictionary<string, object>();
        attributes["class"] = "nav-link" + (DebugPopup ? " active" : "");
        return attributes;
    }

    public IDictionary<string, object> NavLinkAttributes3()
    {
        Dictionary<string, object> attributes = new Dictionary<string, object>();
        attributes["class"] = "nav-link" + (FileEncryptionProperties ? " active" : "");
        return attributes;
    }

    public void CloseSuccessPopup()
    {
        ShowVersion = false;
    }

    private void SetRecentFilesHiddenState(bool hideRecentFiles)
    {
        ObservableCollection<FileDetails> recentFilesList = new ObservableCollection<FileDetails>();
        if (!hideRecentFiles)
        {
            recentFilesList = new ObservableCollection<FileDetails>(_mainViewModel!.RecentFiles.Select(f => new FileDetails(f)));
        }

        _recentFilesViewModel.IsHideRecentFiles = hideRecentFiles;
        _recentFilesViewModel!.RecentFilesList = recentFilesList;
        _recentFilesViewModel.UpdateViewState();

        HideRecentFiles = hideRecentFiles;
        New<UserSettings>().HideRecentFiles = hideRecentFiles;
    }

    public async Task ToggleEncryptionUpgradeMode()
    {
        if (_mainViewModel!.EncryptionUpgradeMode == EncryptionUpgradeMode.AutoUpgrade)
        {
            _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.RetainWithoutUpgrade;
            AutoUpgradeToAES256 = false;
            UpdateViewState();
            return;
        }

        if (!await New<IVerifySignInPassword>().Verify(Texts.LegacyConversionVerificationPrompt))
        {
            AutoUpgradeToAES256 = false;
            UpdateViewState();
            return;
        }

        _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.AutoUpgrade;
        AutoUpgradeToAES256 = true;
        UpdateViewState();
    }

    public async void ToggleIncludeSubfolders(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.IncludeSubfolders, (ss, ee) => { return ToggleIncludeSubfoldersOption(); }, null!, e);
    }

    private async Task ToggleIncludeSubfoldersOption()
    {
        if (_mainViewModel!.FolderOperationMode == FolderOperationMode.IncludeSubfolders)
        {
            _mainViewModel.FolderOperationMode = FolderOperationMode.SingleFolder;
            IncludeSubfolders = false;
            UpdateViewState();
            return;
        }

        if (!await New<IVerifySignInPassword>().Verify(Texts.ChangeOptionGenericWarning))
        {
            IncludeSubfolders = false;
            UpdateViewState();
            return;
        }

        PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.IncludeSubfoldersConfirmationTitle, Texts.IncludeSubfoldersConfirmationBody);
        IncludeSubfolders = false;
        if (result == PopupButtons.Ok)
        {
            _mainViewModel.FolderOperationMode = FolderOperationMode.IncludeSubfolders;
            IncludeSubfolders = true;
            UpdateViewState();
        }

        UpdateViewState();
    }

    public async Task OnInactivityContextMenu(int duration, EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.InactivitySignOut, async (ss, ee) =>
        {
            SelectedOption = duration;
            New<UserSettings>().InactivitySignOutTime = TimeSpan.FromMinutes(int.Parse(duration.ToString()));
            InactivitySignOut = (int)New<UserSettings>().InactivitySignOutTime.TotalMinutes;
        }, null!, e);

        StartInactivitySignOut();
    }

    private void StartInactivitySignOut()
    {
        if (!_logOnViewModel.UserHas(LicenseCapability.InactivitySignOut))
        {
            return;
        }

        TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime, _fileOperationViewModel!.IdentityViewModel));
        New<InactivitySignOut>().RestartInactivityTimer();
    }

    public async void RestoreRename(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel!.RestoreRandomRenameFiles.ExecuteAsync(null!); }, null!, args);
    }

    public void ToggleAlwaysOffline(EventArgs e)
    {
        AlwaysOffline = !New<UserSettings>().OfflineMode;
        New<UserSettings>().OfflineMode = AlwaysOffline;
        New<AxCryptOnlineState>().IsOffline = AlwaysOffline;
        _logOnViewModel.UIStateChanged();
    }

    public void ToggleDebug() => UpdateDebugMode(!_mainViewModel.DebugMode);

    public bool EnableUserActivity { get; set; }

    public void ToggleUserActivity() => UpdateUserActivityMode(!New<UserSettings>().UserActivityMode);

    public bool EnableFindFiles { get; set; }

    public void ToggleFindFiles() => UpdateFindFileMode(!New<UserSettings>().FindFileMode);

    public void FilePropertiesDateModified(EventArgs e)
    {
        New<UserSettings>().EncryptFilePropertiesDateModified = !IsDateModifiedOn;
    }

    public void FilePropertiesFileName(EventArgs e)
    {
        New<UserSettings>().EncryptFilePropertiesFileName = !IsFileNameOn;
    }

    public async void InviteUser(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { _logOnViewModel.InviteDialog.Show(); }, null!, e);
    }

    public void ToggleAdvancedOption()
    {
        if (!_mainViewModel!.LoggedOn)
        {
            return;
        }

        _logOnViewModel.AdvancedOptionsDialog.Show();
    }

    public void ToggleVaultSettings()
    {
        if (!_mainViewModel!.LoggedOn)
        {
            return;
        }

        AxCServiceProvider.GetService<VaultSettingsViewModel>().VaultSettingsDialog.Show();
    }

    public void SwitchDarkMode()
    {
        DarkTheme = !New<UserSettings>().DarkMode;
        New<UserSettings>().DarkMode = DarkTheme;
    }

    #region Debug Section

    public void UpdateDebugMode(bool enabled)
    {
        EnableDebugPopup = enabled;
        _mainViewModel!.DebugMode = enabled;
    }

    private void UpdateUserActivityMode(bool enabled)
    {
        New<UserSettings>().UserActivityMode = enabled;
        AxCServiceProvider.GetService<ProfileViewModel>().UpdateViewState();
    }

    private void UpdateFindFileMode(bool enabled)
    {
        New<UserSettings>().FindFileMode = enabled;
        AxCServiceProvider.GetService<ProfileViewModel>().UpdateViewState();
    }

    public string? ErrorMessage { get; set; }
    public string TimeoutInput { get; set; } = string.Empty;
    public TimeSpan TimeoutTimeSpan { get; private set; }
    public string? RestApiBaseUrlInput { get; set; }
    public Uri? RestApiBaseUrl { get; private set; }

    private ObservableCollection<ManageAccountModel> AccountEmailsList { get; set; } = new ObservableCollection<ManageAccountModel>();
    public string? EmailLabel { get; set; }

    private bool _userInitiatedUpdateCheckPending = false;

    public async void CheckAxCryptVersionAsync()
    {
        _userInitiatedUpdateCheckPending = true;
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel!.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }

    public string? VersionHoverText { get; set; }
    public bool ShowUpdate { get; set; }

    public void OpenOptions()
    {
        ShowOptions = true;
    }

    public bool TryValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(RestApiBaseUrlInput) || !Uri.TryCreate(RestApiBaseUrlInput, UriKind.RelativeOrAbsolute, out Uri? uri))
        {
            ErrorMessage = "Invalid API base URL.";
            return false;
        }

        if (!TimeSpan.TryParse(TimeoutInput, out TimeSpan timeout))
        {
            ErrorMessage = "Invalid timeout format. Use format like 00:02:30 for 2 mins 30 secs.";
            return false;
        }

        RestApiBaseUrl = uri;
        TimeoutTimeSpan = timeout;

        return true;
    }

    public void SetOptionsToolStripMenuItem_Click(EventArgs e)
    {
        if (!TryValidateInputs())
        {
            return;
        }

        Resolve.UserSettings.RestApiBaseUrl = RestApiBaseUrl!;
        Resolve.UserSettings.ApiTimeout = TimeoutTimeSpan;

        ErrorMessage = string.Empty;
        ShowOptions = false;
        UpdateViewState();
    }

    public void CloseOptions()
    {
        RestApiBaseUrlInput = Resolve.UserSettings.RestApiBaseUrl.ToString();
        TimeoutInput = Resolve.UserSettings.ApiTimeout.ToString();

        ErrorMessage = string.Empty;
        ShowOptions = false;
        UpdateViewState();
    }

    public void OnOpenLogViewerClicked(LogType logType)
    {
        _logViewModel!.IsDebugLogsVisible = true;
        New<IDebugLoggingWindow>().ShowLogWindow(logType);
    }

    public async void OpenManageAxCryptID()
    {
        AccountStorage userKeyPairs = new AccountStorage(New<LogOnIdentity, IAccountService>(Resolve.KnownIdentities.DefaultEncryptionIdentity));
        _viewModel = await ManageAccountViewModel.CreateAsync(userKeyPairs);
        //_viewModel.BindPropertyChanged<IEnumerable<AccountProperties>>(nameof(ManageAccountViewModel.AccountProperties), ListAccountEmails);
        ListAccountEmails(_viewModel.AccountProperties);
        ShowManageAxCryptID = true;
    }

    private void ListAccountEmails(IEnumerable<AccountProperties> emails)
    {
        foreach (AccountProperties email in emails)
        {
            ManageAccountModel item = new ManageAccountModel(email.Timestamp.ToLocalTime().ToString(CultureInfo.CurrentCulture), "Timestamp");

            AccountEmailsList.Add(item);
        }

        if (AccountEmailsList.Count == 0)
        {
            EmailLabel = String.Empty;
            return;
        }

        EmailLabel = emails.First().EmailAddress;
        CreatedTime = emails.First().Timestamp;
    }

    public void ChangePassphraseButton_Click(EventArgs e)
    {
        New<UserSettings>().UserEmail.ProcessChangePassword();
        ShowManageAxCryptID = false;
    }

    public void CloseManageAxCryptID()
    {
        ShowManageAxCryptID = false;
    }

    public async void OpenBrokenFiles()
    {
        await _fileOperationViewModel!.TryBrokenFiles.ExecuteAsync(null!);
    }

    public async void VerifyAxCryptFiles()
    {
        await _fileOperationViewModel!.VerifyFiles.ExecuteAsync(null!);
    }

    public async void AxCryptFileFormatCheck()
    {
        await _fileOperationViewModel!.IntegrityCheckFiles.ExecuteAsync(null!);
    }

    public void OpenReportAsync()
    {
        New<IReport>().Open();
    }

    #endregion Debug Section

    private void ConfigureMenusAccordingToPolicy(LicenseCapabilities license)
    {
        ConfigureAutoUpgradeMenu(license);
        ConfigureIncludeSubfoldersMenu(license);
        ConfigureInactivityTimeOutMenu(license);
        ConfigureRestoreRenameMenu(license);
        ConfigureEncryptionFilePropertiesMenu(license);
        ConfigureInviteFrientMenu(license);
        ConfigureFindFilesMenu(license);
        ConfigureVaultMenu(license);
        UpdateViewState();
    }

    private void ConfigureAutoUpgradeMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.StrongerEncryption))
        {
            EnableAutoUpgrade = true;
        }
        else
        {
            EnableAutoUpgrade = false;
        }
    }

    private void ConfigureIncludeSubfoldersMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.IncludeSubfolders))
        {
            EnableIncludeSubfolders = true;
        }
        else
        {
            EnableIncludeSubfolders = false;
        }
    }

    private void ConfigureInactivityTimeOutMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.InactivitySignOut))
        {
            EnableInActivitySignOut = true;
        }
        else
        {
            EnableInActivitySignOut = false;
        }
    }

    private void ConfigureRestoreRenameMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.RandomRename))
        {
            EnableRestoreRename = true;
        }
        else
        {
            EnableRestoreRename = false;
        }
    }

    private void ConfigureEncryptionFilePropertiesMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.RandomRename))
        {
            EnableEncryptionFileProperties = true;
        }
        else
        {
            EnableEncryptionFileProperties = false;
        }
    }

    private void ConfigureInviteFrientMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.KeySharing))
        {
            EnableInviteFriend = true;
        }
        else
        {
            EnableInviteFriend = false;
        }
    }

    private void ConfigureFindFilesMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.FindFiles))
        {
            EnableFindFilesMenuOption = true;
        }
        else
        {
            EnableFindFilesMenuOption = false;
        }
    }

    private void ConfigureVaultMenu(LicenseCapabilities license)
    {
        if (license.Has(LicenseCapability.Vault))
        {
            EnableVaultMenuOption = true;
        }
        else
        {
            EnableVaultMenuOption = false;
        }
    }

    private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
    {
        if (_logOnViewModel.UserHas(requiredCapability))
        {
            if (realHandler != null)
            {
                await realHandler(sender, e);
            }
            return;
        }

        ShowUpgradePopup();
    }

    public void ShowUpgradePopup()
    {
        AxCServiceProviderExtension.UpgradeSubscriptionViewModel!.ShowUpgradeDialog();
    }
}