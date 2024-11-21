using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Utility;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class CreateNewAccountDialogViewModel : ViewModelBase
{
    private CreateNewAccountViewModel _viewModel;
    private LogOnViewModel _logOnViewModel;
    public bool IsCreating = false;

    public CreateNewAccountDialogViewModel(LogOnViewModel logOnViewModel)
    {
        _logOnViewModel = logOnViewModel;
    }

    public void SetCreateNewAccount(string passPhrase, EmailAddress email)
    {
        _viewModel = new CreateNewAccountViewModel(passPhrase, email);

        _logOnViewModel.CreateNewAccountDialog.Show();
    }

    private void CreateNewAccountDialog_Load(object s, EventArgs ee)
    {
        //_viewModel.BindPropertyChanged(nameof(CreateNewAccountViewModel.ShowPassword), (bool show) => { !(ShowPassword = show); });
        _viewModel.BindPropertyChanged(nameof(CreateNewAccountViewModel.PasswordText), (string p) => { PassphraseText = p; });
        _viewModel.BindPropertyChanged(nameof(CreateNewAccountViewModel.Verification), (string v) => { VerifyPassPhraseText = v; });
        _viewModel.BindPropertyChanged(nameof(CreateNewAccountViewModel.UserEmail), (string u) => { UserEmail = u; });
    }

    public string ErrorMessage { get; set; }

    public bool ShowPassword { get; set; }

    public string PassphraseText { get; set; }

    public string VerifyPassPhraseText { get; set; }

    public string UserEmail { get; set; }

    public DialogResult DialogResult { get; set; }

    public async void ButtonOk_Click(EventArgs e)
    {
        if (IsCreating || !AdHocValidationDueToMonoLimitations())
        {
            DialogResult = DialogResult.None;
            return;
        }

        if (!New<KeyPairService>().IsAnyAvailable)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.OfflineAccountTitle, Texts.OfflineAccountBePatient);
        }

        CreateAccountAsync();
    }

    private async void CreateAccountAsync()
    {
        IsCreating = true;

        try
        {
            New<AxCryptOnlineState>().IsOnline = false;
            TaskRunner.WaitFor(() => _viewModel.CreateAccount.ExecuteAsync(null));
        }
        finally
        {
            IsCreating = false;
        }

        DialogResult = DialogResult.OK;
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = AdHocValidateAllFieldsIndependently();
        return validated;
    }

    private bool AdHocValidateAllFieldsIndependently()
    {
        return AdHocValidatePassphrase() & AdHocValidateVerfication() & AdHocValidateUserEmail();
    }

    private bool AdHocValidatePassphrase()
    {
        ErrorMessage = "";
        if (_viewModel[nameof(CreateNewAccountViewModel.PasswordText)].Length > 0)
        {
            ErrorMessage = Texts.PasswordPolicyViolation;
            return false;
        }
        return true;
    }

    private bool AdHocValidateVerfication()
    {
        ErrorMessage = "";
        if (_viewModel[nameof(CreateNewAccountViewModel.Verification)].Length > 0)
        {
            ErrorMessage = Texts.PassphraseVerificationMismatch;
            return false;
        }
        return true;
    }

    private bool AdHocValidateUserEmail()
    {
        ErrorMessage = "";
        if (_viewModel[nameof(CreateNewAccountViewModel.UserEmail)].Length > 0)
        {
            ErrorMessage = Texts.BadEmail;
            return false;
        }
        return true;
    }

    public async void HelpButton_Click()
    {
        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.DialogVerifyAccountTitle, Texts.PasswordRulesInfo);
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}
