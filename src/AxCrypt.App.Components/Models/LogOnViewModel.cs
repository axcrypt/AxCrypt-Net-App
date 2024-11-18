using AxCrypt.Api.Model;
using AxCrypt.App.Components.Utility;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Components.Models;

public interface ILogOnDialogService
{
    event Action<bool>? OnLogOnDialogVisibilityChanged;

    bool IsVisible { get; }

    //void ShowDialog();
}

public class LogOnViewModel : ViewModelBase
{
    public LogOnViewModel()
    {
        License = New<LicensePolicy>().Capabilities;
    }

    public void ShowLogOnDialog(LogOnAccountViewModel logOnAccountModel, MainViewModel mainViewModel)
    {
        LogOnAccountModel = logOnAccountModel;
        IsVisible = true;

        while (PageResult == DialogResult.None)
        {
            Task.Delay(1000);
        }
        IsVisible = false;

        //mainViewModel.BindPropertyChanged(nameof(mainViewModel.LoggedOn), (bool loggedOn) => { IsVisible = !loggedOn; });
        mainViewModel.BindPropertyChanged(nameof(mainViewModel.License), (LicenseCapabilities license) => { if (license != null) { License = license; OnLogOnDialogVisibilityChanged?.Invoke(_isVisible); } });
    }

    public MainViewModel MainViewModel { get { return GetProperty<MainViewModel>(nameof(MainViewModel)); } set { SetProperty(nameof(MainViewModel), value); } }

    public FileOperationViewModel FileOperationViewModel { get { return GetProperty<FileOperationViewModel>(nameof(FileOperationViewModel)); } set { SetProperty(nameof(FileOperationViewModel), value); } }


    public DialogResult PageResult { get { return GetProperty<DialogResult>(nameof(PageResult)); } set { SetProperty(nameof(PageResult), value); } }

    public LicenseCapabilities License { get { return GetProperty<LicenseCapabilities>(nameof(License)); } set { SetProperty(nameof(License), value); } }

    public SubscriptionLevel SubscriptionLevel
    {
        get
        {
            return License.GetLicenseStatus();
        }
    }

    public LogOnAccountViewModel LogOnAccountModel { get; set; }

    public string ErrorMessage { get; set; }

    public event Action<bool>? OnLogOnDialogVisibilityChanged;

    public Func<Task>? OnLogOnOrLogOffAndLogOnAgain;

    private bool _isVisible;

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