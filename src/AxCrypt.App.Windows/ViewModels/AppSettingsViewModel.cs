using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
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

using static AxCrypt.Abstractions.TypeResolve;
using AxCrypt.App.Components.Models;

namespace AxCrypt.App.Windows.ViewModels;

public class AppSettingsViewModel
{
    private LogOnViewModel _logOnViewModel;
    private MainViewModel? _mainViewModel;
    private FileOperationViewModel? _fileOperationViewModel;
    private ManageAccountViewModel? _viewModel;
    private IExportKeyManagementFile? ExportKeyFile;
    private bool hideRecentFiles;
    private bool isDateModifiedOn;
    private bool isFileNameOn;

    public AppSettingsViewModel(LogOnViewModel logOnViewModel)
    {
        _logOnViewModel = logOnViewModel;
        _mainViewModel = _logOnViewModel.MainViewModel;
        _fileOperationViewModel = _logOnViewModel.FileOperationViewModel;
    }

    public void Initialized()
    {
        RestApiBaseUrl = Resolve.UserSettings.RestApiBaseUrl.ToString();
        TimeoutTimeSpan = Resolve.UserSettings.ApiTimeout.ToString();
        
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FolderOperationMode), (FolderOperationMode SecureFolderLevel) => { IncludeSubfolders = SecureFolderLevel == FolderOperationMode.IncludeSubfolders ? true : false; });
    }

    public bool InactSgnOut { get; set; }
    public bool FileEncryptionProperties { get; set; }
    public Uri? DownloadVersion { get; set; }
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool ShowVersion { get; set; }
    public bool ShowOptions { get; set; }
    public bool ShowManageAxCryptID { get; set; }
    public bool InvitePopup { get; set; }
    public ObservableCollection<FileDetails> RecentFilesList { get; set; } = new ObservableCollection<FileDetails>();
    public DateTime CreatedTime { get; set; }
    public double SelectedOption { get; set; } = 0;
    public bool DebugPopup { get; set; }

    protected bool isHovered = false;
    protected string hoveredElement = string.Empty;

    public bool HideRecentFiles
    {
        get => hideRecentFiles;
        set
        {
            if (hideRecentFiles != value)
            {
                hideRecentFiles = value;
                ToggleHideRecentFiles();
            }
        }
    }

    public bool ShowAdvancedOptions { get; set; }

    public bool IsChecked(int value) => SelectedOption == value;

    protected void ShowPopup(string element) => isHovered = true;

    protected void HidePopup() => isHovered = false;

    public void ToggleDebugPopup() => DebugPopup = !DebugPopup;

    public void ToggleInvitePopup() => InvitePopup = !InvitePopup;

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
        ShowVersion = !ShowVersion;
    }

    private void SetRecentFilesHiddenState(bool hideRecentFiles)
    {
        New<UserSettings>().HideRecentFiles = hideRecentFiles;

        if (!hideRecentFiles)
        {
            RecentFilesList = new ObservableCollection<FileDetails>(_mainViewModel.RecentFiles.Select(f => new FileDetails(f)));
            return;
        }

        RecentFilesList.Clear();
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
        }
    }

    public void OnInactivityContextMenu(int duration)
    {
        bool hasFeature = New<LicensePolicy>().Capabilities.Has(LicenseCapability.InactivitySignOut);

        if (hasFeature)
        {
            SelectedOption = duration;
            New<UserSettings>().InactivitySignOutTime = TimeSpan.FromMinutes(int.Parse(duration.ToString()));
            TypeMap.Register.Singleton<InactivitySignOut>(() => new InactivitySignOut(New<UserSettings>().InactivitySignOutTime));
            New<InactivitySignOut>().RestartInactivityTimer();
        }
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
    }

    public void FilePropertiesDateModified(EventArgs e)
    {
        New<UserSettings>().EncryptFilePropertiesDateModified = !New<UserSettings>().EncryptFilePropertiesDateModified;
    }

    public void FilePropertiesFileName(EventArgs e)
    {
        New<UserSettings>().EncryptFilePropertiesFileName = !New<UserSettings>().EncryptFilePropertiesFileName;
    }

    public async void InviteUser(EventArgs e)
    {
        await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { InvitePopup = !InvitePopup; }, null, e);
    }

    public async Task ToggleAdvancedOption()
    {
        if (!_mainViewModel.LoggedOn)
        {
            return;
        }

        ShowAdvancedOptions = !ShowAdvancedOptions;
    }

    #region Debug Section

    private string? Title { get; set; }
    private string? DefaultExt { get; set; }
    private string? Filter { get; set; }
    private bool AddExtension { get; set; }
    private string? FileName { get; set; }
    public string? RestApiBaseUrl { get; set; }
    public string? TimeoutTimeSpan { get; set; }
    private ObservableCollection<ManageAccountModel> _accountEmailsListView { get; set; } = new ObservableCollection<ManageAccountModel>();
    public string? EmailLabel { get; set; }

    public bool IsDateModifiedOn
    {
        get => isDateModifiedOn;
        set
        {
            if (isDateModifiedOn != value)
            {
                isDateModifiedOn = value;
                UpdateDateModifiedSetting();
            }
        }
    }

    public bool IsFileNameOn
    {
        get => isFileNameOn;
        set
        {
            if (isFileNameOn != value)
            {
                isFileNameOn = value;
                UpdateFileNameSetting();
            }
        }
    }

    private bool includeSubfolders;

    public bool IncludeSubfolders
    {
        get => includeSubfolders;
        set
        {
            if (includeSubfolders != value)
            {
                includeSubfolders = value;
                IncludeSubFoldersChanged();
            }
        }
    }

    private bool alwaysOnline;

    public bool AlwaysOffline
    {
        get => alwaysOnline;
        set
        {
            if (alwaysOnline != value)
            {
                alwaysOnline = value;
                //MakeAlwaysOffline();
            }
        }
    }

    private bool autoUpgradeToAES256;

    public bool AutoUpgradeToAES256
    {
        get => autoUpgradeToAES256;
        set
        {
            if (autoUpgradeToAES256 != value)
            {
                autoUpgradeToAES256 = value;
            }
        }
    }

    public async void CheckAxCryptVersionAsync()
    {
        ShowVersion = !ShowVersion;

        await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue);
    }

    public void OpenOptions()
    {
        ShowOptions = true;
    }

    public void SetOptionsToolStripMenuItem_Click(EventArgs e)
    {
        Resolve.UserSettings.RestApiBaseUrl = new Uri(RestApiBaseUrl);
        Resolve.UserSettings.ApiTimeout = TimeSpan.Parse(TimeoutTimeSpan);

        ShowOptions = !ShowOptions;
    }

    public void CloseOptions()
    {
        ShowOptions = !ShowOptions;
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

            _accountEmailsListView.Add(item);
        }

        if (_accountEmailsListView.Count == 0)
        {
            EmailLabel = String.Empty;
            return;
        }

        EmailLabel = emails.First().EmailAddress;
    }

    public void ChangePassphraseButton_Click(EventArgs e)
    {
        New<UserSettings>().UserEmail.ProcessChangePassword();
        ShowManageAxCryptID = !ShowManageAxCryptID;
    }

    public void CloseManageAxCryptID()
    {
        ShowManageAxCryptID = !ShowManageAxCryptID;
    }

    private void UpdateDateModifiedSetting()
    {
        New<UserSettings>().EncryptFilePropertiesDateModified = IsDateModifiedOn;
    }

    private void UpdateFileNameSetting()
    {
        New<UserSettings>().EncryptFilePropertiesFileName = IsFileNameOn;
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

    private async Task<IEnumerable<FileResult>> InternalFileSelectionAsync(FileSelectionEventArgs e)
    {
        IEnumerable<FileResult> pickResult = await FilePicker.PickMultipleAsync(new PickOptions
        {
            PickerTitle = "Please select files",
        });

        if (!pickResult.Any())
        {
            e.Cancel = true;
        }

        return pickResult;
    }

    public void HandleSelection(FileSelectionEventArgs e)
    {
        if (e == null)
        {
            throw new ArgumentNullException(nameof(e));
        }

        HandleSelectionInternal(e);
    }

    private void HandleSelectionInternal(FileSelectionEventArgs e)
    {
        switch (e.FileSelectionType)
        {
            case FileSelectionType.SaveAsEncrypted:
            case FileSelectionType.SaveAsDecrypted:
                HandleSaveAsFileSelection(e);
                break;

            case FileSelectionType.WipeConfirm:
                //HandleWipeConfirm(e);
                break;

            case FileSelectionType.Folder:
                //HandleFolderSelection(e);
                break;

            default:
                //HandleOpenFileSelection(e);
                break;
        }
    }

    private async void HandleSaveAsFileSelection(FileSelectionEventArgs e)
    {
        switch (e.FileSelectionType)
        {
            case FileSelectionType.SaveAsEncrypted:
                Title = Texts.EncryptFileSaveAsDialogTitle;
                DefaultExt = OS.Current.AxCryptExtension;
                AddExtension = true;
                Filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + DefaultExt, Texts.FileFilterFileTypeAxCryptFiles, Texts.FileFilterFileTypeAllFiles);
                break;

            case FileSelectionType.SaveAsDecrypted:
                string extension = Path.GetExtension(e.SelectedFiles[0]);
                Title = Texts.DecryptedSaveAsFileDialogTitle;
                DefaultExt = extension;
                AddExtension = !string.IsNullOrEmpty(extension);
                Filter = Texts.FileFilterDialogFilterPatternWin.InvariantFormat("." + DefaultExt, Texts.FileFilterFileTypeFiles, Texts.FileFilterFileTypeAllFiles);
                break;
        }

        FileName = Path.GetFileName(e.SelectedFiles[0]);
        string savedPath = await ExportKeyFile.ShowSaveFileDialogAsync(Title, DefaultExt, Filter, FileName);

        Core.Service.UserKeyPair activeKeyPair = Core.Resolve.KnownIdentities.DefaultEncryptionIdentity.ActiveEncryptionKeyPair;
        Core.UI.EmailAddress userEmail = activeKeyPair.UserEmail;
        Core.Crypto.Asymmetric.IAsymmetricPublicKey publicKey = activeKeyPair.KeyPair.PublicKey;

        byte[] export = activeKeyPair.ToArray(Resolve.KnownIdentities.DefaultEncryptionIdentity.Passphrase);

        if (!string.IsNullOrEmpty(savedPath))
        {
            await File.WriteAllBytesAsync(savedPath, export);
        }
    }

    public async void IncludeSubFoldersChanged()
    {
        await _mainViewModel.SetFolderOperationMode(IncludeSubfolders ? FolderOperationMode.IncludeSubfolders : FolderOperationMode.SingleFolder);
    }

    public void MakeAlwaysOffline()
    {
        New<UserSettings>().OfflineMode = AlwaysOffline;

        if (AlwaysOffline)
        {
            New<AxCryptOnlineState>().IsOffline = true;
            return;
        }

        New<AxCryptOnlineState>().IsOffline = false;
    }

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

        //showUpgradePopup = true;
    }
}
