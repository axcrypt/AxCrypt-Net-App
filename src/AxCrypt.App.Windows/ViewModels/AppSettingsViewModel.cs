using AxCrypt.Abstractions;
using AxCrypt.App.Components.ViewModels;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.ObjectModel;
using AxCrypt.Core.Extensions;
using System.Globalization;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.App.Components.Services.Interface;
using AxCrypt.Core.Runtime;
using AxCrypt.App.Windows.Models;
using AxCrypt.App.Components.Models;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class AppSettingsViewModel : ViewModelBase
{
    private LogOnViewModel _logOnViewModel;
    private MainViewModel? _mainViewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private ManageAccountViewModel? _viewModel;
    private IExportKeyManagementFile? ExportKeyFile;
    private RecentFilesViewModel? _recentFilesViewModel;
    private bool hideRecentFiles;
    private bool isDateModifiedOn;
    private bool isFileNameOn;

    public AppSettingsViewModel(LogOnViewModel logOnViewModel, RecentFilesViewModel recentFilesViewModel)
    {
        _logOnViewModel = logOnViewModel;
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
    }

    public void Initialized()
    {
        RestApiBaseUrl = Resolve.UserSettings.RestApiBaseUrl.ToString();
        TimeoutTimeSpan = Resolve.UserSettings.ApiTimeout.ToString();

        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FolderOperationMode), (FolderOperationMode SecureFolderLevel) => { IncludeSubfolders = SecureFolderLevel == FolderOperationMode.IncludeSubfolders ? true : false; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptionUpgradeMode), (EncryptionUpgradeMode mode) => AutoUpgradeToAES256 = mode == EncryptionUpgradeMode.AutoUpgrade);
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { UpdateRecentFiles(files); _logOnViewModel.UIStateChanged(); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { _userInitiatedUpdateCheckPending = true; await DisplayUpdateCheckPopups(); });
    }

    public List<int> InactivityTimeoutOptions { get; } = new List<int> { 0, 5, 15, 30, 60 };

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

    public bool ShowAdvancedOptions { get; set; }

    public int InactivitySignOut { get; set; }

    public void ToggleDebugPopup() => DebugPopup = !DebugPopup;

    public void ToggleHideRecentFiles() => SetRecentFilesHiddenState(!New<UserSettings>().HideRecentFiles);

    public void InactSgnOutPopup() => InactSgnOut = !InactSgnOut;

    public void FileEncryptionProperty() => FileEncryptionProperties = !FileEncryptionProperties;

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
        New<UserSettings>().HideRecentFiles = hideRecentFiles;

        if (!hideRecentFiles)
        {
            _recentFilesViewModel.RecentFilesList = new ObservableCollection<FileDetails>(_mainViewModel.RecentFiles.Select(f => new FileDetails(f)));
            _logOnViewModel.UIStateChanged();
            return;
        }

        _recentFilesViewModel.RecentFilesList.Clear();
        _logOnViewModel.UIStateChanged();
    }

    void UpdateRecentFiles(IEnumerable<ActiveFile> recentFiles)
    {
        _recentFilesViewModel.RecentFilesList = new ObservableCollection<FileDetails>(recentFiles.Select(f => new FileDetails(f)));
    }

    public void ToggleEncryptionUpgradeMode()
    {
        if (_mainViewModel.EncryptionUpgradeMode == EncryptionUpgradeMode.AutoUpgrade)
        {
            _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.RetainWithoutUpgrade;
            return;
        }

        if (!New<IVerifySignInPassword>().Verify(Texts.LegacyConversionVerificationPrompt))
        {
            return;
        }

        _mainViewModel.EncryptionUpgradeMode = EncryptionUpgradeMode.AutoUpgrade;
    }

    public async void ToggleIncludeSubfolders(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.IncludeSubfolders, (ss, ee) => { return ToggleIncludeSubfoldersOption(); }, null, e);
    }

    private async Task ToggleIncludeSubfoldersOption()
    {
        if (_mainViewModel.FolderOperationMode == FolderOperationMode.IncludeSubfolders)
        {
            _mainViewModel.FolderOperationMode = FolderOperationMode.SingleFolder;
            IncludeSubfolders = false;
            return;
        }

        if (!New<IVerifySignInPassword>().Verify(Texts.ChangeOptionGenericWarning))
        {
            return;
        }

        PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.IncludeSubfoldersConfirmationTitle, Texts.IncludeSubfoldersConfirmationBody);
        if (result == PopupButtons.Ok)
        {
            _mainViewModel.FolderOperationMode = FolderOperationMode.IncludeSubfolders;
            IncludeSubfolders = true;
        }
    }

    public async Task OnInactivityContextMenu(int duration, EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.InactivitySignOut, async (ss, ee) =>
        {
            SelectedOption = duration;
            New<UserSettings>().InactivitySignOutTime = TimeSpan.FromMinutes(int.Parse(duration.ToString()));
            InactivitySignOut = New<UserSettings>().InactivitySignOutTime.Minutes;
            TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
            New<InactivitySignOut>().RestartInactivityTimer();
        }, null, e);
    }

    public async void RestoreRename(EventArgs args)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.RandomRename, async (ss, ee) => { await _fileOperationViewModel.RestoreRandomRenameFiles.ExecuteAsync(null); }, null, args);
    }

    public void ToggleAlwaysOffline(EventArgs e)
    {
        AlwaysOffline = !New<UserSettings>().OfflineMode;
        New<UserSettings>().OfflineMode = AlwaysOffline;
        New<AxCryptOnlineState>().IsOffline = AlwaysOffline;
        _logOnViewModel.UIStateChanged();
    }

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
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { _logOnViewModel.InviteDialog.Show(); }, null, e);
    }

    public async Task ToggleAdvancedOption()
    {
        if (!_mainViewModel.LoggedOn)
        {
            return;
        }

        ShowAdvancedOptions = true;
    }

    #region Debug Section

    public string? RestApiBaseUrl { get; set; }
    public string? TimeoutTimeSpan { get; set; }
    private ObservableCollection<ManageAccountModel> AccountEmailsList { get; set; } = new ObservableCollection<ManageAccountModel>();
    public string? EmailLabel { get; set; }

    private bool _userInitiatedUpdateCheckPending = false;

    public async void CheckAxCryptVersionAsync()
    {
        ShowVersion = true;
        await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
    }

    private async Task DisplayUpdateCheckPopups()
    {
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }

    public void OpenOptions()
    {
        ShowOptions = true;
    }

    public void SetOptionsToolStripMenuItem_Click(EventArgs e)
    {
        Resolve.UserSettings.RestApiBaseUrl = new Uri(RestApiBaseUrl);
        Resolve.UserSettings.ApiTimeout = TimeSpan.Parse(TimeoutTimeSpan);

        ShowOptions = false;
    }

    public void CloseOptions()
    {
        ShowOptions = false;
    }

    public void OnOpenLogViewerClicked()
    {
        Window newWindow = new Window(new LogViewerWindow());
        Application.Current?.OpenWindow(newWindow);
    }

    public async void OpenManageAxCryptID()
    {
        AccountStorage userKeyPairs = new AccountStorage(New<LogOnIdentity, IAccountService>(Resolve.KnownIdentities.DefaultEncryptionIdentity));
        _viewModel = await ManageAccountViewModel.CreateAsync(userKeyPairs);
        _viewModel.BindPropertyChanged<IEnumerable<AccountProperties>>(nameof(ManageAccountViewModel.AccountProperties), ListAccountEmails);

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
        await _fileOperationViewModel.TryBrokenFiles.ExecuteAsync(null);
    }

    public async void VerifyAxCryptFiles()
    {
        await _fileOperationViewModel.VerifyFiles.ExecuteAsync(null);
    }

    public async void AxCryptFileFormatCheck()
    {
        await _fileOperationViewModel.IntegrityCheckFiles.ExecuteAsync(null);
    }

    public void OpenReportAsync()
    {
        New<IReport>().Open();
    }

    #endregion

    private async Task PremiumFeature_ClickAsync(LicenseCapability requiredCapability, Func<object, EventArgs, Task> realHandler, object sender, EventArgs e)
    {
        if (_mainViewModel.License.Has(requiredCapability))
        {
            if (realHandler != null)
            {
                await realHandler(sender, e);
            }
            return;
        }

        _logOnViewModel.UpgradeDialog.Show();
    }
}
