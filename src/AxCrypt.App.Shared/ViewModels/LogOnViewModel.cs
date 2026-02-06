using AxCrypt.Api.Model;
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
        UpgradeDialog = new CommonDialogService();
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

    public async Task ShowLogOnDialog(LogOnAccountViewModel logOnAccountModel, MainViewModel mainViewModel)
    {
        mainViewModel.BindPropertyChanged(nameof(mainViewModel.LoggedOn), async (bool loggedOn) =>
        {
            if (loggedOn)
            {
                ProcessIndicator?.Dispose();
                await New<AccountStatusViewModel>().LoadAccountStatusAsync();
            }
        });

        mainViewModel.BindPropertyChanged(nameof(mainViewModel.License), (LicenseCapabilities license) => 
        { 
            if (license != null! && MainViewModel.LoggedOn) 
            {
                OnSubscriptionChanged?.Invoke(); 
            }
        });

        ProcessIndicator?.Dispose();
        ShowGetStartedCarousel = WorkUserProfile.IsFirstSignIn;

        LogOnAccountModel = logOnAccountModel;
        IsVisible = true;

        while (PageResult == DialogResult.None)
        {
            await Task.Delay(1000);
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

    public CommonDialogService UpgradeDialog
    { get { return GetProperty<CommonDialogService>(nameof(UpgradeDialog)); } set { SetProperty(nameof(UpgradeDialog), value); } }

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

    public void SubscriptionChanged()
    {
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