using AxCrypt.Abstractions;
using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Services.UI;
using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Content;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.ViewModels;

public class LogOnViewModel : ViewModelBase
{
    public LogOnViewModel()
    {
        License = New<LicensePolicy>().Capabilities;
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
    }

    public async Task ShowLogOnDialog(LogOnAccountViewModel logOnAccountModel, MainViewModel mainViewModel)
    {
        ShowGetStartedCarousel = AxCrypt.Core.Resolve.UserSettings.IsFirstSignIn;

        LogOnAccountModel = logOnAccountModel;
        IsVisible = true;

        while (PageResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }
        InitiateProgressIndicator();

        ShowGetStartedCarousel = false;
        IsVisible = false;

        mainViewModel.BindPropertyChanged(nameof(mainViewModel.LoggedOn), (bool loggedOn) => { if (loggedOn) { ProcessIndicator?.Dispose(); OnSubscriptionChanged?.Invoke(); } });
        mainViewModel.BindPropertyChanged(nameof(mainViewModel.License), (LicenseCapabilities license) => { if (license != null) { License = license; OnSubscriptionChanged?.Invoke(); } });
    }

    public MainViewModel MainViewModel
    { get { return GetProperty<MainViewModel>(nameof(MainViewModel)); } set { SetProperty(nameof(MainViewModel), value); } }

    public FileOperationViewModel FileOperationViewModel
    { get { return GetProperty<FileOperationViewModel>(nameof(FileOperationViewModel)); } set { SetProperty(nameof(FileOperationViewModel), value); } }

    public DialogResult PageResult
    { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

    public LicenseCapabilities License
    {
        get { return GetProperty<LicenseCapabilities>(nameof(License)); }
        set
        {
            SetProperty(nameof(License), value);
            _subscriptionLevel = License.GetLicenseStatus();
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
    { get { return GetProperty<CommonDialogService>(nameof(AboutDialog)); } set { SetProperty(nameof(AboutDialog), value); } }

    public CommonDialogService FeedbackDialog
    { get { return GetProperty<CommonDialogService>(nameof(FeedbackDialog)); } set { SetProperty(nameof(FeedbackDialog), value); } }
    
    public CommonDialogService GlobalPopupDialog
    { get { return GetProperty<CommonDialogService>(nameof(GlobalPopupDialog)); } set { SetProperty(nameof(GlobalPopupDialog), value); } }
    
    public CommonDialogService AdvancedOptionsDialog
    { get { return GetProperty<CommonDialogService>(nameof(AdvancedOptionsDialog)); } set { SetProperty(nameof(AdvancedOptionsDialog), value); } }

    public ProcessIndicator ProcessIndicator { get; set; }

    public void InitiateProgressIndicator()
    {
        ProcessIndicator = new ProcessIndicator();
    }

    private SubscriptionLevel _subscriptionLevel = SubscriptionLevel.Unknown;

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            if (_subscriptionLevel == SubscriptionLevel.Unknown)
            {
                _subscriptionLevel = License.GetLicenseStatus();
            }

            return _subscriptionLevel;
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

    public string ErrorMessage { get; set; }

    public event Action? OnSubscriptionChanged;

    public event Action<bool>? OnLogOnDialogVisibilityChanged;

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

    private static bool _isTfaEnabled;

    public bool IsTfaEnabled
    {
        get => _isTfaEnabled;
        set
        {
            _isTfaEnabled = value;
            OnLogOnDialogVisibilityChanged?.Invoke(_isVisible);
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

    public async void ClearAllSettingsAndRestartAsync()
    {
        await new ApplicationManager().ClearAllSettings();
        await ShutDownAnd(New<IUIThread>().RestartApplication);
    }

    private async Task ShutDownAnd(Action finalAction)
    {
        await new ApplicationManager().ShutdownBackgroundSafe();
        finalAction();
    }
}