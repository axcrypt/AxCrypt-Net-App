using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.User;
using AxCrypt.Core.UI.ViewModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class LogOnViewModel : ViewModelBase
{
    public LogOnViewModel()
    {
        InviteDialog = new CommonDialogService();
        ShareKeyDialog = new CommonDialogService();
        ImportPrivatePasswordDialog = new CommonDialogService();
        RenewSubscriptionDialog = new CommonDialogService();
        VerifyAccountDialog = new CommonDialogService();
        VerifyPasswordDialog = new CommonDialogService();
        AboutDialog = new CommonDialogService();
        FeedbackDialog = new CommonDialogService();
        GlobalPopupDialog = new CommonDialogService();
        AdvancedOptionsDialog = new CommonDialogService();
        UpgradeVersionDialog = new CommonDialogService();
        FolderSettingsDialog = new CommonDialogService();
        UserPromptDialog = new CommonDialogService();
        SwitchUserDialog = new CommonDialogService();
    }

    // Guards the one-time MainViewModel property subscriptions below.
    // ShowLogOnDialog runs on every sign-in — including every
    // sign-out → sign-in cycle. The handlers used to be re-attached on
    // each call, so after N cycles a single successful sign-in fired N
    // duplicate LoadAccountStatusAsync() network calls (and N
    // InitializeData calls), making each subsequent sign-in slower than
    // the last. Binding once fixes that compounding slowdown.
    private bool _mainViewModelBound;

    public async Task ShowLogOnDialog(LogOnAccountViewModel logOnAccountModel, MainViewModel mainViewModel)
    {
        _subscriptionChangeDetected = false;

        // Set before wiring the handler so the (single) LoggedOn handler
        // always reads the model for the sign-in currently in progress.
        LogOnAccountModel = logOnAccountModel;

        if (!_mainViewModelBound)
        {
            _mainViewModelBound = true;

            mainViewModel.BindPropertyChanged(nameof(mainViewModel.LoggedOn), async (bool loggedOn) =>
            {
                if (loggedOn)
                {
                    IsVisible = false;
                    PageResult = DialogResult.OK;
                    ProcessIndicator?.Dispose();
                    await New<AccountStatusViewModel>().LoadAccountStatusAsync();
                    SubscriptionChanged();

                    AxCServiceProvider.GetService<UserService>().InitializeData(SubscriptionLevel, LogOnAccountModel?.UserEmail ?? string.Empty);
                }
            });

            mainViewModel.BindPropertyChanged(nameof(mainViewModel.License), (LicenseCapabilities license) =>
            {
                if (license != null! && MainViewModel.LoggedOn)
                {
                    SubscriptionChanged();
                }
            });
        }

        ProcessIndicator?.Dispose();
        ShowGetStartedCarousel = WorkUserProfile.IsFirstSignIn;

        IsVisible = true;

        // Poll for the dialog result. 100 ms keeps the hand-off snappy —
        // the old 1000 ms interval added up to a full second of dead wait
        // after the user pressed Sign In, on every sign-in and every
        // sign-out → sign-in.
        while (PageResult == DialogResult.None)
        {
            await Task.Delay(100);
        }

        if (PageResult != DialogResult.Cancel)
        {
            InitiateProgressIndicator();
        }

        ShowGetStartedCarousel = false;
        IsVisible = false;
        MultiFactorAuthViewModel = new MultiFactorAuthViewModel();
    }

    public MainViewModel MainViewModel
    { get { return GetProperty<MainViewModel>(nameof(MainViewModel)); } set { SetProperty(nameof(MainViewModel), value); } }

    public FileOperationViewModel FileOperationViewModel
    { get { return GetProperty<FileOperationViewModel>(nameof(FileOperationViewModel)); } set { SetProperty(nameof(FileOperationViewModel), value); } }

    public DialogResult PageResult
    { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

    public LicenseCapabilities License
    {
        get
        {
            return MainViewModel.License;
        }
    }

    public CommonDialogService InviteDialog
    { get { return GetProperty<CommonDialogService>(nameof(InviteDialog)); } set { SetProperty(nameof(InviteDialog), value); } }

    public CommonDialogService FilePicker
    { get { return GetProperty<CommonDialogService>(nameof(FilePicker)); } set { SetProperty(nameof(FilePicker), value); } }

    public CommonDialogService ShareKeyDialog
    { get { return GetProperty<CommonDialogService>(nameof(ShareKeyDialog)); } set { SetProperty(nameof(ShareKeyDialog), value); } }

    public CommonDialogService ImportPrivatePasswordDialog
    { get { return GetProperty<CommonDialogService>(nameof(ImportPrivatePasswordDialog)); } set { SetProperty(nameof(ImportPrivatePasswordDialog), value); } }

    public CommonDialogService RenewSubscriptionDialog
    { get { return GetProperty<CommonDialogService>(nameof(RenewSubscriptionDialog)); } set { SetProperty(nameof(RenewSubscriptionDialog), value); } }

    public CommonDialogService VerifyAccountDialog
    { get { return GetProperty<CommonDialogService>(nameof(VerifyAccountDialog)); } set { SetProperty(nameof(VerifyAccountDialog), value); } }

    public CommonDialogService VerifyPasswordDialog
    { get { return GetProperty<CommonDialogService>(nameof(VerifyPasswordDialog)); } set { SetProperty(nameof(VerifyPasswordDialog), value); } }

    public CommonDialogService AboutDialog
    {
        get { return GetProperty<CommonDialogService>(nameof(AboutDialog)); }
        set { SetProperty(nameof(AboutDialog), value); }
    }

    public CommonDialogService FeedbackDialog
    { get { return GetProperty<CommonDialogService>(nameof(FeedbackDialog)); } set { SetProperty(nameof(FeedbackDialog), value); } }

    public CommonDialogService GlobalPopupDialog
    { get { return GetProperty<CommonDialogService>(nameof(GlobalPopupDialog)); } set { SetProperty(nameof(GlobalPopupDialog), value); } }

    public CommonDialogService AdvancedOptionsDialog
    { get { return GetProperty<CommonDialogService>(nameof(AdvancedOptionsDialog)); } set { SetProperty(nameof(AdvancedOptionsDialog), value); } }

    public CommonDialogService UpgradeVersionDialog
    { get { return GetProperty<CommonDialogService>(nameof(UpgradeVersionDialog)); } set { SetProperty(nameof(UpgradeVersionDialog), value); } }

    public CommonDialogService FolderSettingsDialog
    { get { return GetProperty<CommonDialogService>(nameof(FolderSettingsDialog)); } set { SetProperty(nameof(FolderSettingsDialog), value); } }

    public CommonDialogService UserPromptDialog
    { get { return GetProperty<CommonDialogService>(nameof(UserPromptDialog)); } set { SetProperty(nameof(UserPromptDialog), value); } }

    public ProcessIndicator ProcessIndicator { get; set; }

    public CommonDialogService SwitchUserDialog
    { get { return GetProperty<CommonDialogService>(nameof(SwitchUserDialog)); } set { SetProperty(nameof(SwitchUserDialog), value); } }

    public void InitiateProgressIndicator()
    {
        ProcessIndicator = new ProcessIndicator();
    }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return License.GetLicenseStatus();
        }
    }

    public bool IsLoggedOn
    {
        get
        {
            return MainViewModel?.LoggedOn ?? false;
        }
    }

    public bool EligibleForFreeTrial
    {
        get
        {
            return New<AccountStatusViewModel>().PlanState.HasFlag(PlanState.CanTryPremium);
        }
    }

    public bool IsOffline
    {
        get
        {
            return New<Common.AxCryptOnlineState>().IsOffline;
        }
    }

    public LogOnAccountViewModel LogOnAccountModel { get; set; }

    public GlobalDialogViewModel? GlobalViewModel { get; set; }

    public PopupButtons[] PopupButtons { get; set; }

    public DialogResult PopupResult
    { get { return GetProperty<DialogResult>(nameof(PopupResult)); } set { SetProperty(nameof(PopupResult), value); } }

    public UpgradeVersionViewModel? UpgradeVersionViewModel { get; set; }

    public string ErrorMessage { get; set; }

    public string CurrentUserDevice { get; set; }

    public bool ShowUpgradeStrongerEncryptionWarning
    {
        get
        {
            return IsLoggedOn && !UserHas(LicenseCapability.StrongerEncryption);
        }
    }

    public bool UserHas(LicenseCapability capability)
    {
        return MainViewModel.License.Has(capability);
    }

    public bool UserInitiatedUpdateCheckPending { get; set; }

    public MultiFactorAuthViewModel MultiFactorAuthViewModel { get; set; }

    public event Action? OnSubscriptionChanged;

    public event Action<bool>? OnLogOnDialogVisibilityChanged;

    public event Action<bool>? OnMFADialogVisibilityChanged;

    public Func<Task>? OnLogOnOrLogOffAndLogOnAgain;

    private static bool _isVisible;

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            OnLogOnDialogVisibilityChanged?.Invoke(_isVisible);
        }
    }

    private static bool _isMfaEnabled;

    public bool IsMfaEnabled
    {
        get => _isMfaEnabled;
        set
        {
            _isMfaEnabled = value;
            OnMFADialogVisibilityChanged?.Invoke(_isMfaEnabled);
        }
    }

    public string SupportPageTitle
    {
        get
        {
            switch (SubscriptionLevel)
            {
                case SubscriptionLevel.Business:
                    return Texts.PrioritySupportTitle;

                case SubscriptionLevel.Premium:
                    return Texts.SupportPageTitle;

                default:
                    return Texts.PromptSupport;
            }
        }
    }

    public bool ShowGetStartedCarousel { get; set; }

    public async Task InvokeLogOnOrLogOffAndLogOnAgainAsync()
    {
        OnLogOnOrLogOffAndLogOnAgain?.Invoke();
    }

    public event Action? OnUIStateChanged;

    public void UIStateChanged()
    {
        OnUIStateChanged?.Invoke();
    }

    private bool _subscriptionChangeDetected = false;

    public void SubscriptionChanged()
    {
        if (_subscriptionChangeDetected)
        {
            return;
        }

        _subscriptionChangeDetected = true;
        OnSubscriptionChanged?.Invoke();
    }

    public async Task AxCryptMainForm_ClickAsync(EventArgs e)
    {
        New<InactivitySignOut>().RestartInactivityTimer();
    }

    public void ShowSwitchUserDialog()
    {
        SwitchUserDialog.Show();
    }
}