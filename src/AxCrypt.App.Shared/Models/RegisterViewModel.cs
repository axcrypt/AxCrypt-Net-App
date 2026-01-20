using AxCrypt.App.Shared.Utility;
using AxCrypt.App.Shared.Utility.View;
using AxCrypt.Common;
using AxCrypt.Content;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Models;

public class RegisterViewModel : ViewModelBase
{
    private bool IsCreating = false;
    
    public event Action<bool>? OnVisibilityOfUserSignUpChanged;

    private bool _showSignUp;

    public bool ShowSignUp
    {
        get => _showSignUp;
        set
        {
            _showSignUp = value;
            OnVisibilityOfUserSignUpChanged?.Invoke(_showSignUp);
        }
    }

    public CreateNewAccountViewModel CreateAccountModel { get; set; } = new CreateNewAccountViewModel(string.Empty, EmailAddress.Empty);

    public DialogResult DialogResult
    { get { return GetProperty<DialogResult>(nameof(DialogResult)); } set { SetProperty(nameof(DialogResult), value); } }

    public string ErrorMessage { get; set; } = string.Empty;

    public async Task ShowDialog(string passphrase, EmailAddress email)
    {
        CreateAccountModel = new CreateNewAccountViewModel(passphrase, email);
        ShowSignUp = true;

        while (DialogResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        ShowSignUp = false;
    }

    public ProcessIndicator? ProcessIndicator { get; set; }

    public async Task ButtonOk_Click(EventArgs e)
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

    private void CreateAccountAsync()
    {
        IsCreating = true;

        try
        {
            New<AxCryptOnlineState>().IsOnline = false;
            TaskRunner.WaitFor(() => CreateAccountModel.CreateAccount.ExecuteAsync(null));
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
        if (CreateAccountModel[nameof(CreateNewAccountViewModel.PasswordText)].Length > 0)
        {
            ErrorMessage = Texts.PasswordPolicyViolation;
            return false;
        }
        return true;
    }

    private bool AdHocValidateVerfication()
    {
        ErrorMessage = "";
        if (CreateAccountModel[nameof(CreateNewAccountViewModel.Verification)].Length > 0)
        {
            ErrorMessage = Texts.PassphraseVerificationMismatch;
            return false;
        }
        return true;
    }

    private bool AdHocValidateUserEmail()
    {
        ErrorMessage = "";
        if (CreateAccountModel[nameof(CreateNewAccountViewModel.UserEmail)].Length > 0)
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