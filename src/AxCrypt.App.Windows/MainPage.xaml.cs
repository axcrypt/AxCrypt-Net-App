using AxCrypt.Abstractions;
using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Components.Services.Interface;
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
using AxCrypt.Core.Session;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows;

public partial class MainPage : ContentPage, ISignIn
{
    //HomeViewModel viewModel;
    private ICustomNavigationService _navigationManager;

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
    public MainPage(MainViewModel mainViewModel, FileOperationViewModel fileOperationViewModel, KnownFoldersViewModel knownFoldersViewModel) : this()
    {
        //_navigationManager = customNavigationService;
        //_mainViewModel = New<MainViewModel>();
        _mainViewModel = mainViewModel;
        _fileOperationViewModel = fileOperationViewModel;
        _knownFoldersViewModel = knownFoldersViewModel;
        //viewModel = homeModel;
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

        SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent();
        await GetApiVersionAsync();
        SetThisVersion();

        UpdateArabicStyle();

        BindToFileOperationViewModel();

        await SignInAsync();
    }

    private void SetupViewModelsAndNotificationsBeforeAnyNotificationsAreSent()
    {
        New<LicensePolicy>();
        _mainViewModel = New<MainViewModel>();
        _fileOperationViewModel = New<FileOperationViewModel>();
        _knownFoldersViewModel = New<KnownFoldersViewModel>();
        New<SessionNotify>().AddCommand(async (notification) => await New<SessionNotificationHandler>().HandleNotificationAsync(notification));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
    private void BindToFileOperationViewModel()
    {
        //_fileOperationViewModel.FirstLegacyOpen += (sender, e) => New<IUIThread>().SendTo(async () => await SetLegacyOpenMode(e));
        _fileOperationViewModel.IdentityViewModel.LoggingOnAsync = async (e) => await New<IUIThread>().SendToAsync(async () => await HandleLogOn(e));

    }

    public bool IsSigningIn { get; set; }

    public async Task SignIn()
    {
        await _fileOperationViewModel.IdentityViewModel.LogOnAsync.ExecuteAsync(null);
    }

    private async Task SignInAsync()
    {
        SignUpSignIn signUpSignIn = new SignUpSignIn(_navigationManager)
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
        _navigationManager.NavigateTo("/login");

        //LogOnAccountViewModel viewModel = new LogOnAccountViewModel(Resolve.UserSettings, e.EncryptedFileFullName);
        //using (SignUpSignInAccountDialog logOnDialog = new SignUpSignInAccountDialog(this, viewModel))
        //{
        //    DialogResult dialogResult = logOnDialog.ShowDialog(this);

        //    if (dialogResult == DialogResult.Retry)
        //    {
        //        await ResetAllSettingsAndRestart();
        //    }

        //    if (dialogResult == DialogResult.Cancel)
        //    {
        //        await new ApplicationManager().StopAndExit();
        //    }

        //    if (dialogResult != DialogResult.OK || viewModel.PasswordText.Length == 0)
        //    {
        //        e.Cancel = true;
        //        return;
        //    }

        //    e.Passphrase = new Passphrase(viewModel.PasswordText);
        //    e.UserEmail = viewModel.UserEmail;
        //}
        return;
    }


    private static void SetThisVersion()
    {
        New<UserSettings>().ThisVersion = New<IVersion>().Current.ToString();
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