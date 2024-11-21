using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Windows.Code;
using AxCrypt.App.Windows.Components.Pages.PopupDialog;
using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System.Globalization;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows;

public partial class MainPage : ContentPage, ISignIn
{
    //HomeViewModel viewModel;
    private ICustomNavigationService _navigationManager;
    private HomeUserService _homeUserService;
    private LogOnViewModel _logOnService;

    private MainViewModel _mainViewModel;
    private FileOperationViewModel _fileOperationViewModel;
    private KnownFoldersViewModel _knownFoldersViewModel;

    private ApiVersion _apiVersion;

    public MainPage()
    {
        InitializeComponent();
        //new Styling(Resources.axcrypticon).Style(this, _recentFilesContextMenuStrip, _watchedFoldersContextMenuStrip);
    }

    //public MainPage(NavigationManager navigationManager, HomeViewModel homeModel) : this()
    public MainPage(HomeUserService homeUserService, LogOnViewModel logOnService, MainViewModel mainViewModel, FileOperationViewModel fileOperationViewModel, KnownFoldersViewModel knownFoldersViewModel) : this()
    {
        //_navigationManager = customNavigationService;
        //_mainViewModel = New<MainViewModel>();
        _homeUserService = homeUserService;
        _logOnService = logOnService;
        _mainViewModel = mainViewModel;
        _fileOperationViewModel = fileOperationViewModel;
        _knownFoldersViewModel = knownFoldersViewModel;
    }

    protected override void OnAppearing()
    {
        Task.Run(async () => await InitializeMainPage());

        base.OnAppearing();
    }


    protected override void ChangeVisualState()
    {
        base.ChangeVisualState();
    }

    private async Task InitializeMainPage()
    {
        New<IRuntimeEnvironment>().FirstInstanceIsReady();

        await GetApiVersionAsync();
        SetThisVersion();

        UpdateArabicStyle();

        BindToViewModels();
        BindToFileOperationViewModel();

        _logOnService.MainViewModel = _mainViewModel;
        _logOnService.FileOperationViewModel = _fileOperationViewModel;

        await SignInAsync();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToViewModels()
    {
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DebugMode), (bool enabled) => { UpdateDebugMode(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DecryptFileEnabled), (bool enabled) => { _decryptToolStripMenuItem.Enabled = enabled; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.DownloadVersion), async (DownloadVersion dv) => { await SetSoftwareStatus(); await DisplayUpdateCheckPopups(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripButton.Enabled = enabled; ConfigureEncryptMenu(enabled); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.EncryptFileEnabled), (bool enabled) => { _encryptToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _cleanDecryptedToolStripMenuItem.Enabled = filesArePending; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.FilesArePending), (bool filesArePending) => { _closeAndRemoveOpenFilesToolStripButton.Visible = filesArePending; _closeAndRemoveOpenFilesToolStripButton.ToolTipText = filesArePending ? Texts.CloseAndRemoveOpenFilesToolStripButtonToolTipText : string.Empty; });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => await _knownFoldersViewModel.UpdateState.ExecuteAsync(null));
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureMenusAccordingToPolicyAsync(license); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.License), async (LicenseCapabilities license) => { await SetWindowTitleTextAsync(_mainViewModel.LoggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.License), (LicenseCapabilities license) => { _recentFilesListView.UpdateRecentFiles(_mainViewModel.RecentFiles); });
        //_mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await ConfigureLinkLabelAsync(New<KnownIdentities>().DefaultEncryptionIdentity); });
        _mainViewModel.BindPropertyAsyncChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { if (loggedOn) New<InactivitySignOut>().RestartInactivityTimer(); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await SetSignInSignOutStatusAsync(loggedOn); });
        _mainViewModel.BindPropertyChanged(nameof(_mainViewModel.LoggedOn), async (bool loggedOn) => { await new Display().LocalSignInWarningPopUpAsync(loggedOn); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.OpenEncryptedEnabled), (bool enabled) => { _openEncryptedToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RandomRenameEnabled), (bool enabled) => { _renameToolStripMenuItem.Enabled = enabled; });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.RecentFiles), (IEnumerable<ActiveFile> files) => { _recentFilesListView.UpdateRecentFiles(files); ShowRecentFilesBackgroundImage(); });
        //_mainViewModel.BindPropertyChanged(nameof(_mainViewModel.WatchedFoldersEnabled), (bool enabled) => { ConfigureWatchedFoldersMenus(enabled); });
        //_checkForUpdateToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_debugCheckVersionNowToolStripMenuItem.Click += async (sender, e) => { _userInitiatedUpdateCheckPending = true; await _mainViewModel.AxCryptUpdateCheck.ExecuteAsync(DateTime.MinValue); };
        //_mainToolStripTableLayout.DragOver += async (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = await GetEffectsForMainToolStripAsync(e); };
        //_optionsDebugToolStripMenuItem.Click += (sender, e) => { _mainViewModel.DebugMode = !_mainViewModel.DebugMode; };
        //_recentFilesListView.ColumnClick += (sender, e) => { SetSortOrder(e.Column); };
        //_recentFilesListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForRecentFiles(e); };
        //_recentFilesListView.MouseClick += (sender, e) => { if (e.Button == MouseButtons.Right) _recentFilesContextMenuStrip.Show((Control)sender, e.Location); };
        //_recentFilesListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedRecentFiles = _recentFilesListView.SelectedItems.Cast<ListViewItem>().Select(lvi => RecentFilesListView.EncryptedPath(lvi)); };
        //_watchedFoldersListView.DragDrop += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.SecureFolders, (ss, ee) => { return _mainViewModel.AddWatchedFolders.ExecuteAsync(_mainViewModel.DragAndDropFiles); }, sender, e); };
        //_watchedFoldersListView.DragOver += (sender, e) => { _mainViewModel.DragAndDropFiles = e.GetDragged(); e.Effect = GetEffectsForWatchedFolders(e); };
        //_watchedFoldersListView.MouseDown += (sender, e) => { if (e.Button == MouseButtons.Right) { ShowHideWatchedFoldersContextMenuItems(e.Location); _watchedFoldersContextMenuStrip.Show((Control)sender, e.Location); } };
        //_watchedFoldersListView.SelectedIndexChanged += (sender, e) => { _mainViewModel.SelectedWatchedFolders = _watchedFoldersListView.SelectedItems.Cast<ListViewItem>().Select(lvi => lvi.Text); };
        //_getPremiumToolStripMenuItem.Click += async (sender, e) => { await New<PremiumManager>().BuyPremium(New<KnownIdentities>().DefaultEncryptionIdentity); };

        //_documentsToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_oneDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_googleDriveToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
        //_dropBoxToolStripButton.Click += async (sender, e) => { KnownFolder_OnClick(sender, e); };
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToFileOperationViewModel()
    {
        //_encryptToolStripButton.Tag = _fileOperationViewModel.EncryptFiles;
        _fileOperationViewModel.FirstLegacyOpen += (sender, e) => New<IUIThread>().SendTo(async () => await SetLegacyOpenMode(e));
        // _fileOperationViewModel.IdentityViewModel.LoggingOnAsync = async (e) => await New<IUIThread>().SendToAsync(async () => await HandleLogOn(e));
        _fileOperationViewModel.IdentityViewModel.LoggingOnAsync = async (e) => await HandleLogOn(e);
        _logOnService.OnLogOnOrLogOffAndLogOnAgain = async () => New<IUIThread>().SendTo(async () => await LogOnOrLogOffAndLogOnAgainAsync());
        _fileOperationViewModel.SelectingFiles += (sender, e) => New<IUIThread>().SendTo(() => New<IDataItemSelection>().HandleSelection(e));
        _fileOperationViewModel.ToggleEncryptionUpgradeMode += (sender, e) => New<IUIThread>().SendTo(() => ToggleEncryptionUpgradeMode());
        //_inviteUserToolStripMenuItem.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.KeySharing, async (ss, ee) => { await InviteUserAsync(); }, sender, e); };
        //_recentFilesListView.DragDrop += async (sender, e) => { await DropFilesOrFoldersInRecentFilesListViewAsync(); };
        //_secretsToolStripButton.Click += async (sender, e) => { await PremiumFeature_ClickAsync(LicenseCapability.PasswordManagement, (ss, ee) => { BrowseUtility.RedirectToSecretsUrl(Resolve.KnownIdentities.DefaultEncryptionIdentity.UserEmail.Address); return Task.FromResult<object>(null); }, sender, e); };
    }


    private async Task LogOnOrLogOffAndLogOnAgainAsync()
    {
        bool wasLoggedOn = Resolve.KnownIdentities.IsLoggedOn;
        if (wasLoggedOn)
        {
            await _fileOperationViewModel.IdentityViewModel.LogOnLogOff.ExecuteAsync(null);
        }
        else
        {
            await SignInAsync();
        }
        bool didLogOff = wasLoggedOn && !Resolve.KnownIdentities.IsLoggedOn;
        if (didLogOff)
        {
            await SignInAsync();
        }
    }

    public bool IsSigningIn { get; set; }

    public async Task SignIn()
    {
        await _fileOperationViewModel.IdentityViewModel.LogOnAsync.ExecuteAsync(null);
    }

    private async Task SignInAsync()
    {
        SignUpSignIn signUpSignIn = new SignUpSignIn(_navigationManager, _homeUserService)
        {
            Version = _apiVersion,
            UserEmail = New<UserSettings>().UserEmail,
        };

        await signUpSignIn.DialogsAsync(this);

        New<UserSettings>().UserEmail = signUpSignIn.UserEmail;

        if (signUpSignIn.StopAndExit)
        {
            await new ApplicationManager().StopAndExit();
            return;
        }

        await SetSignInSignOutStatusAsync(_mainViewModel.LoggedOn);
        if (_mainViewModel.LoggedOn && Thread.CurrentThread.CurrentUICulture.Name != Resolve.UserSettings.CultureName)
        {
            await SetLanguageAsync(Resolve.UserSettings.CultureName);
        }

        ShowRenewSubscriptionDialog();
    }

    private async Task SetSignInSignOutStatusAsync(bool isSignedIn)
    {
        await SetWindowTitleTextAsync(isSignedIn);
        bool isSignedInWithAxCryptId = New<KnownIdentities>().IsLoggedOnWithAxCryptId;

        //_createAccountToolStripMenuItem.Enabled = !isSignedIn;
        //_debugManageAccountToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportMyPrivateKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_exportSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_importMyPrivateKeyToolStripMenuItem.Enabled = !isSignedIn;
        //_importOthersSharingKeyToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_inviteUserToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline && isSignedIn;
        //_optionsEncryptionUpgradeModeToolStripMenuItem.Enabled = isSignedInWithAxCryptId;
        //_optionsChangePassphraseToolStripMenuItem.Enabled = New<AxCryptOnlineState>().IsOnline;
        //_passwordResetToolStripMenuItem.Enabled = !isSignedIn && !string.IsNullOrEmpty(New<UserSettings>().UserEmail);
        //_signInToolStripMenuItem.Visible = !isSignedIn;
        //_notifySignInToolStripMenuItem.Visible = !isSignedIn;
        //_signOutToolStripMenuItem.Visible = isSignedIn;
        //_notifySignOutToolStripMenuItem.Visible = isSignedIn;
        //_encryptionUpgradeMenuItem.Enabled = isSignedInWithAxCryptId;
    }

    private async Task SetWindowTitleTextAsync(bool isLoggedOn)
    {
        try
        {
            string windowTitle = await new Display().WindowTitleTextAsync(isLoggedOn);
            //Application.Current.Windows.First().SetValue(Window.TitleProperty, windowTitle);
            Application.Current.Windows.First().Title = windowTitle;
            //App.Current.Windows.FirstOrDefault().Title = windowTitle;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private async Task SetLanguageAsync(string cultureName)
    {
        Resolve.UserSettings.CultureName = cultureName;
        if (Resolve.Log.IsInfoEnabled)
        {
            Resolve.Log.LogInfo("Set new UI language culture to '{0}'.".InvariantFormat(Resolve.UserSettings.CultureName));
        }

        UpdateArabicStyle();

        InitializeContentResources();
        await SetWindowTitleTextAsync(_mainViewModel.LoggedOn);
        //_daysLeftPremiumLabel.UpdateText();
        await SetSoftwareStatus();
    }

    private void UpdateArabicStyle()
    {
        //if (Resolve.UserSettings.CultureName == "ar-AR")
        //{
        //    this.RightToLeft = RightToLeft.Yes;
        //    return;
        //}

        //this.RightToLeft = RightToLeft.No;
    }

    private async Task SetSoftwareStatus()
    {
        //_softwareStatusButton.Image = Resources.bulb_green_40px;
        //_softwareStatusButton.Visible = true;
        //VersionUpdateStatus status = _mainViewModel.VersionUpdateStatus;
        //switch (status)
        //{
        //    case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
        //    case VersionUpdateStatus.IsUpToDate:
        //        _softwareStatusButton.Visible = false;
        //        break;

        //    case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
        //        _softwareStatusButton.ToolTipText = Texts.OldVersionTooltip;
        //        break;

        //    case VersionUpdateStatus.NewerVersionIsAvailable:
        //        _softwareStatusButton.ToolTipText = Texts.NewVersionIsAvailableText.InvariantFormat(_mainViewModel.DownloadVersion.Version) + ' ' + Texts.ClickToDownloadText;
        //        break;

        //    case VersionUpdateStatus.Unknown:
        //        _softwareStatusButton.ToolTipText = Texts.ClickToCheckForNewerVersionTooltip;
        //        break;
        //}
    }

    private void InitializeContentResources()
    {
        SetCulture();
    }

    private static void SetCulture()
    {
        if (String.IsNullOrEmpty(Resolve.UserSettings.CultureName))
        {
            return;
        }

        Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(Resolve.UserSettings.CultureName);
    }

    private void ShowRenewSubscriptionDialog()
    {
        if (!_mainViewModel.LoggedOn || !AxCryptUserAccountViewModel.HadAnyPaidSubscription)
        {
            return;
        }

        _logOnService.RenewSubscriptionDialog.Show();
        //using (RenewSubscriptionPromptDialog dialog = new RenewSubscriptionPromptDialog(this))
        //{
        //    if (dialog.HideDialog)
        //    {
        //        return;
        //    }

        //    if (dialog.ShowDialog(this) != DialogResult.OK)
        //    {
        //        return;
        //    }
        //}
    }

    private async Task GetApiVersionAsync()
    {
        try
        {
            _apiVersion = await New<ICache>().GetItemAsync(CacheKey.RootKey.Subkey("WrapMessageDialogsAsync_ApiVersion"), () => New<GlobalApiClient>().ApiVersionAsync(Environment.OSVersion.VersionString, New<AboutAssembly>().AssemblyVersion));
        }
        catch (ApiException aex)
        {
            await aex.HandleApiExceptionAsync();
            _apiVersion = ApiVersion.Zero;
        }
    }

    private void UpdateKnownFolders(IEnumerable<KnownFolder> folders)
    {
        foreach (KnownFolder folder in folders)
        {
            //GetIconClass(folder.My.FullName);
        }
    }

    private async Task HandleLogOn(LogOnEventArgs e)
    {
        if (e.IsAskingForPreviouslyUnknownPassphrase)
        {
            HandleCreateNewLogOn(e);
        }
        else
        {
            await HandleExistingLogOn(e);
        }
        if (New<UserSettings>().RestoreFullWindow)
        {
            //Styling.RestoreWindowWithFocus(this);
        }
    }

    private void HandleCreateNewLogOn(LogOnEventArgs e)
    {
        if (!String.IsNullOrEmpty(e.EncryptedFileFullName))
        {
            HandleCreateNewLogOnForEncryptedFile(e);
        }
        else
        {
            HandleCreateNewAccount(e);
        }
    }

    private void HandleCreateNewLogOnForEncryptedFile(LogOnEventArgs e)
    {
        NewPasswordViewModel viewModel = new NewPasswordViewModel(e.Passphrase.Text, e.EncryptedFileFullName);

        //using (NewPassphraseDialog passphraseDialog = new NewPassphraseDialog(this, Texts.NewPassphraseDialogTitle, viewModel))
        //{
        //    viewModel.ShowPassword = e.DisplayPassphrase;
        //    DialogResult dialogResult = passphraseDialog.ShowDialog(this);
        //    e.DisplayPassphrase = viewModel.ShowPassword;
        //    if (dialogResult != DialogResult.OK || viewModel.PasswordText.Length == 0)
        //    {
        //        e.Cancel = true;
        //        return;
        //    }
        //    e.Passphrase = new Passphrase(viewModel.PasswordText);
        //    e.Name = String.Empty;
        //}
        return;
    }

    private void HandleCreateNewAccount(LogOnEventArgs e)
    {
        CreateNewAccountDialogViewModel createNewAccountDialogViewModel = new CreateNewAccountDialogViewModel(_logOnService);
        createNewAccountDialogViewModel.SetCreateNewAccount(e.Passphrase.Text, e.Identity.UserEmail);
        //using (CreateNewAccountDialog dialog = new CreateNewAccountDialog(this, e.Passphrase.Text, EmailAddress.Empty))
        //{
        //    DialogResult dialogResult = dialog.ShowDialog(this);
        //    if (dialogResult != DialogResult.OK)
        //    {
        //        e.Cancel = true;
        //        return;
        //    }
        //    e.DisplayPassphrase = dialog.ShowPassphraseCheckBox.Checked;
        //    e.Passphrase = new Passphrase(dialog.PassphraseTextBox.Text);
        //    e.UserEmail = dialog.EmailTextBox.Text;
        //}
    }

    private async Task HandleExistingLogOn(LogOnEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.EncryptedFileFullName) && (string.IsNullOrEmpty(Resolve.UserSettings.UserEmail) || Resolve.KnownIdentities.IsLoggedOn))
        {
            HandleExistingLogOnForEncryptedFile(e);
        }
        else
        {
            await HandleExistingAccountLogOn(e);
        }
    }

    private void HandleExistingLogOnForEncryptedFile(LogOnEventArgs e)
    {
        FilePasswordDialogViewModel filePasswordDialogViewModel = new FilePasswordDialogViewModel(_logOnService);
        filePasswordDialogViewModel.SetFilePassword(e.EncryptedFileFullName);
        //using (FilePasswordDialog logOnDialog = new FilePasswordDialog(this, e.EncryptedFileFullName))
        //{
        //    DialogResult dialogResult = logOnDialog.ShowDialog(this);
        //    if (dialogResult == DialogResult.Retry)
        //    {
        //        e.Passphrase = logOnDialog.ViewModel.Passphrase;
        //        e.IsAskingForPreviouslyUnknownPassphrase = true;
        //        return;
        //    }

        //    if (dialogResult != DialogResult.OK || logOnDialog.ViewModel.Passphrase == Passphrase.Empty)
        //    {
        //        e.Cancel = true;
        //        return;
        //    }
        //    e.Passphrase = logOnDialog.ViewModel.Passphrase;
        //}
        return;
    }

    private async Task HandleExistingAccountLogOn(LogOnEventArgs e)
    {
        if (!_logOnService.IsVisible)
        {
            LogOnAccountViewModel logOnModel = new LogOnAccountViewModel(Resolve.UserSettings, e.EncryptedFileFullName);
        }

        if (_logOnService.PageResult == DialogResult.None)
        {
            return;
        }

        if (_logOnService.PageResult == DialogResult.Retry)
        {
            await ResetAllSettingsAndRestart();
        }

        if (_logOnService.PageResult == DialogResult.Cancel)
        {
            await new ApplicationManager().StopAndExit();
        }

        if (_logOnService.PageResult != DialogResult.OK || _logOnService.LogOnAccountModel.PasswordText.Length == 0)
        {
            e.Cancel = true;
            return;
        }

        e.Passphrase = new Passphrase(_logOnService.LogOnAccountModel.PasswordText);
        e.UserEmail = _logOnService.LogOnAccountModel.UserEmail;
        _logOnService.PageResult = DialogResult.None;
        //LogOnAccountViewModel viewModel = new LogOnAccountViewModel(Resolve.UserSettings, e.EncryptedFileFullName);
        //using (SignUpSignInAccountDialog logOnDialog = new SignUpSignInAccountDialog(this, viewModel))
        //{
        //    DialogResult dialogResult = logOnDialog.ShowDialog(this);
        //}
        return;
    }

    private static void SetThisVersion()
    {
        New<UserSettings>().ThisVersion = New<IVersion>().Current.ToString();
    }
    private async Task ResetAllSettingsAndRestart()
    {
        if (_mainViewModel.DecryptedFiles.Any())
        {
            await _mainViewModel.WarnIfAnyDecryptedFiles.ExecuteAsync(null);
            return;
        }

        PopupButtons result = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle, Texts.ResetAllSettingsWarningText);
        if (result == PopupButtons.Ok)
        {
            new ApplicationManager().WaitForBackgroundToComplete();
            await new ApplicationManager().ClearAllSettings();
            await new ApplicationManager().ShutdownBackgroundSafe();

            New<IUIThread>().RestartApplication();
        }
    }


    private static async Task SetLegacyOpenMode(FileOperationEventArgs e)
    {
        if (!Resolve.KnownIdentities.IsLoggedOn)
        {
            return;
        }

        PopupButtons click = await New<IPopup>().ShowAsync(PopupButtons.OkCancel, Texts.WarningTitle, Texts.LegacyOpenMessage);
        if (click == PopupButtons.Cancel)
        {
            e.Cancel = true;
            return;
        }
    }

    private void ToggleEncryptionUpgradeMode()
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

    private bool _userInitiatedUpdateCheckPending = false;

    private async Task DisplayUpdateCheckPopups()
    {
        await new Display().UpdateCheckPopups(_userInitiatedUpdateCheckPending, _mainViewModel.DownloadVersion);
        _userInitiatedUpdateCheckPending = false;
    }

    private void UpdateDebugMode(bool enabled)
    {
        //_optionsDebugToolStripMenuItem.Checked = enabled;
        //_debugToolStripMenuItem.Visible = enabled;
    }

    private void ImportMyPrivateKeyToolStripMenuItem_Click(object sender, EventArgs e)
    {
        _logOnService.ImportPrivatePasswordDialog.Show();
    }
}