using AxCrypt.Api.Model;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Services.UI;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Components.Utility.View;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models;

public class LogOnViewModel : ViewModelBase
{
    private ProcessIndicatorService _processIndicatorService;

    public LogOnViewModel(ProcessIndicatorService processIndicatorService)
    {
        License = New<LicensePolicy>().Capabilities;
        InviteDialog = new CommonDialogService();
        UpgradeDialog = new CommonDialogService();
        ShareKeyDialog = new CommonDialogService();
        ImportPrivatePasswordDialog = new CommonDialogService();
        RenewSubscriptionDialog = new CommonDialogService();
        CreateNewAccountDialog = new CommonDialogService();
        FilePasswordDialog = new CommonDialogService();
        VerifyAccountDialog = new CommonDialogService();
        VerifyPasswordDialog = new CommonDialogService();
        AboutDialog = new CommonDialogService();
        FeedbackDialog = new CommonDialogService();
        _processIndicatorService = processIndicatorService;
    }

    public void ShowLogOnDialog(LogOnAccountViewModel logOnAccountModel, MainViewModel mainViewModel)
    {
        LogOnAccountModel = logOnAccountModel;
        IsVisible = true;

        while (PageResult == DialogResult.None)
        {
            Task.Delay(1000);
        }
        InitiateProgressIndicator();

        IsVisible = false;

        mainViewModel.BindPropertyChanged(nameof(mainViewModel.LoggedOn), (bool loggedOn) => { if (loggedOn) { ProcessIndicator?.Dispose(); }/* else { IsVisible = !loggedOn; }*/ });
        mainViewModel.BindPropertyChanged(nameof(mainViewModel.License), (LicenseCapabilities license) => { if (license != null) { License = license; OnSubscriptionChanged?.Invoke(); } });
    }

    public MainViewModel MainViewModel { get { return GetProperty<MainViewModel>(nameof(MainViewModel)); } set { SetProperty(nameof(MainViewModel), value); } }

    public FileOperationViewModel FileOperationViewModel { get { return GetProperty<FileOperationViewModel>(nameof(FileOperationViewModel)); } set { SetProperty(nameof(FileOperationViewModel), value); } }

    public DialogResult PageResult { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

    public LicenseCapabilities License { get { return GetProperty<LicenseCapabilities>(nameof(License)); } set { SetProperty(nameof(License), value); } }

    public CommonDialogService InviteDialog { get { return GetProperty<CommonDialogService>(nameof(InviteDialog)); } set { SetProperty(nameof(InviteDialog), value); } }

    public CommonDialogService UpgradeDialog { get { return GetProperty<CommonDialogService>(nameof(UpgradeDialog)); } set { SetProperty(nameof(UpgradeDialog), value); } }

    public CommonDialogService ShareKeyDialog { get { return GetProperty<CommonDialogService>(nameof(ShareKeyDialog)); } set { SetProperty(nameof(ShareKeyDialog), value); } }

    public CommonDialogService ImportPrivatePasswordDialog { get { return GetProperty<CommonDialogService>(nameof(ImportPrivatePasswordDialog)); } set { SetProperty(nameof(ImportPrivatePasswordDialog), value); } }

    public CommonDialogService RenewSubscriptionDialog { get { return GetProperty<CommonDialogService>(nameof(RenewSubscriptionDialog)); } set { SetProperty(nameof(RenewSubscriptionDialog), value); } }

    public CommonDialogService CreateNewAccountDialog { get { return GetProperty<CommonDialogService>(nameof(CreateNewAccountDialog)); } set { SetProperty(nameof(CreateNewAccountDialog), value); } }

    public CommonDialogService FilePasswordDialog { get { return GetProperty<CommonDialogService>(nameof(FilePasswordDialog)); } set { SetProperty(nameof(FilePasswordDialog), value); } }

    public CommonDialogService VerifyAccountDialog { get { return GetProperty<CommonDialogService>(nameof(VerifyAccountDialog)); } set { SetProperty(nameof(VerifyAccountDialog), value); } }

    public CommonDialogService VerifyPasswordDialog { get { return GetProperty<CommonDialogService>(nameof(VerifyPasswordDialog)); } set { SetProperty(nameof(VerifyPasswordDialog), value); } }

    public CommonDialogService AboutDialog { get { return GetProperty<CommonDialogService>(nameof(AboutDialog)); } set { SetProperty(nameof(AboutDialog), value); } }

    public CommonDialogService FeedbackDialog { get { return GetProperty<CommonDialogService>(nameof(FeedbackDialog)); } set { SetProperty(nameof(FeedbackDialog), value); } }

    public ProcessIndicator ProcessIndicator { get; set; }

    private void InitiateProgressIndicator()
    {
        ProcessIndicator = new ProcessIndicator(_processIndicatorService);
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
            return MainViewModel.LoggedOn;
        }
    }

    public LogOnAccountViewModel LogOnAccountModel { get; set; }

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

    //public async Task HandleValidSubmit(LoginModel login)
    //{
    //    Loading = true;
    //    try
    //    {
    //        if (New<UserSettings>().RememberMe != login.RememberMe)
    //        {
    //            New<UserSettings>().RememberMe = login.RememberMe;
    //        }

    //        EmailAddress userEmail = ValidUserEmail(login);
    //        if (userEmail == EmailAddress.Empty)
    //        {
    //            Loading = false;
    //            return;
    //        }

    //        AccountStatus status = AccountStatus.Verified;
    //        //AccountStatus status = await New<MainHomeViewModel>().SignIn(userEmail, login.Password);
    //        switch (status)
    //        {
    //            case AccountStatus.Verified:
    //            case AccountStatus.DefinedByServer:
    //                NavigateToHomePage();
    //                break;
    //            default:
    //                login.ErrorMessage = Texts.LoginError;
    //                break;
    //        }

    //        Loading = false;
    //    }
    //    catch (Exception ex)
    //    {
    //        throw new Exception(ex.Message, ex);
    //    }
    //}


    //private EmailAddress ValidUserEmail(LoginModel loginModel)
    //{
    //    loginModel.Email = loginModel.Email.Trim();
    //    if (string.IsNullOrEmpty(loginModel.Email))
    //    {
    //        loginModel.ErrorMessage = Texts.InvalidEmail;
    //        return EmailAddress.Empty;
    //    }

    //    if (!EmailAddress.TryParse(loginModel.Email, out EmailAddress parsedEmail))
    //    {
    //        loginModel.ErrorMessage = Texts.InvalidEmail;
    //        return EmailAddress.Empty;
    //    }

    //    if (New<UserSettings>().RememberMe)
    //    {
    //        New<UserSettings>().UserEmail = parsedEmail.Address;
    //    }

    //    return parsedEmail;
    //}
}