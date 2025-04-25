using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.Extensions;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.ViewModels;

public class VerifyAccountDialogViewModel
{
    private VerifyAccountViewModel? _viewModel;
    private LogOnViewModel _logOnViewModel;

    public VerifyAccountDialogViewModel()
    {
        _logOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
    }

    public void SetVerifyAccount(VerifyAccountViewModel viewModel)
    {
        _viewModel = viewModel;
        _logOnViewModel.VerifyAccountDialog.Show();
    }

    private void VerifyAccountDialog_Load(object s, EventArgs ee)
    {
        //_passphrase.TextChanged += (sender, e) => { _viewModel.PasswordText = _passphrase.Text; ClearErrorProviders(); };
        //_passphrase.TextChanged += async (sender, e) => { await _passwordStrengthMeter.MeterAsync(_passphrase.Text); };
        //_passphraseVerification.TextChanged += (sender, e) => { _viewModel.Verification = _passphraseVerification.Text; ClearErrorProviders(); };
        //_activationCode.TextChanged += (sender, e) => { _viewModel.VerificationCode = _activationCode.Text; ClearErrorProviders(); };
        //_showPassphrase.CheckedChanged += (sender, e) => { _viewModel.ShowPassword = ShowPassword; };

        _viewModel!.BindPropertyChanged(nameof(VerifyAccountViewModel.ShowPassword), (bool show) => { ShowPassword = show; });
        _viewModel.BindPropertyChanged(nameof(VerifyAccountViewModel.UserEmail), (string u) => { PromptUserEmail = Texts.MessageSigningUpText.InvariantFormat(u); });
    }

    public string? ErrorMessage { get; set; }

    public bool ShowPassword { get; set; }

    public string? ActivationCode { get; set; }

    public string? PassPhrase { get; set; }

    public string? PassPhraseVerification { get; set; }

    public DialogResult DialogResult { get; set; }

    public string? PromptUserEmail { get; set; }

    public async void ButtonOk_Click(EventArgs e)
    {
        if (string.IsNullOrEmpty(PassPhrase) && string.IsNullOrEmpty(ActivationCode) && string.IsNullOrEmpty(PassPhraseVerification))
        {
            return;
        }

        _viewModel!.VerificationCode = ActivationCode!;
        _viewModel.Verification = PassPhraseVerification!;
        _viewModel.PasswordText = PassPhrase!;

        DialogResult = DialogResult.None;
        if (await IsAllValid())
        {
            DialogResult = DialogResult.OK;
        }
    }

    private async Task<bool> IsAllValid()
    {
        await _viewModel!.CheckAccountStatus.ExecuteAsync(null!);
        if (_viewModel.AlreadyVerified)
        {
            return true;
        }

        if (!AdHocValidationDueToMonoLimitations())
        {
            return false;
        }

        await _viewModel.VerifyAccount.ExecuteAsync(null!);
        if (!VerifyCode())
        {
            return false;
        }

        return true;
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = AdHocValidateAllFieldsIndependently();
        return validated;
    }

    private bool AdHocValidateAllFieldsIndependently()
    {
        return AdHocValidatePassphrase() & AdHocValidatePassphraseVerification() & AdHocValidateCode();
    }

    private bool AdHocValidatePassphrase()
    {
        ErrorMessage = "";
        if (_viewModel![nameof(VerifyAccountViewModel.PasswordText)].Length > 0)
        {
            ErrorMessage = Texts.PasswordPolicyViolation;
            return false;
        }
        return true;
    }

    private bool AdHocValidatePassphraseVerification()
    {
        ErrorMessage = "";
        if (_viewModel![nameof(VerifyAccountViewModel.Verification)].Length > 0)
        {
            ErrorMessage = Texts.PassphraseVerificationMismatch;
            return false;
        }
        return true;
    }

    private bool AdHocValidateCode()
    {
        ErrorMessage = "";
        if (_viewModel![nameof(VerifyAccountViewModel.VerificationCode)].Length > 0)
        {
            ErrorMessage = Texts.WrongVerificationCodeFormat;
            return false;
        }
        return true;
    }

    private bool VerifyCode()
    {
        ErrorMessage = "";
        if (_viewModel![nameof(VerifyAccountDialogViewModel.ErrorMessage)].Length > 0)
        {
            ErrorMessage = Texts.WrongVerificationCode;
            return false;
        }
        return true;
    }

    public void ResendButton_Click(EventArgs e)
    {
        UriBuilder url = new UriBuilder(Texts.ResendActivationHyperLink);
        url.Query = $"email={_viewModel!.UserEmail}";
        Process.Start(url.ToString());
    }

    public async void HelpButton_Click(EventArgs e)
    {
        await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.DialogVerifyAccountTitle, Texts.PasswordRulesInfo);
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}