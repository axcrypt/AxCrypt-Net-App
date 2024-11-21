using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Utility;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.ViewModels;

public class VerifyPasswordViewModel
{
    private VerifySignInPasswordViewModel _viewModel;

    private string _verifyInstructionText;

    public LogOnViewModel LogOnViewModel { get; set; }

    public VerifyPasswordViewModel(LogOnViewModel logOnViewModel)
    {
        LogOnViewModel = logOnViewModel;
        _viewModel = new VerifySignInPasswordViewModel(New<KnownIdentities>().DefaultEncryptionIdentity);
    }

    public bool SetViewPassword(VerifySignInPasswordViewModel viewModel, string verifyInstructionText)
    {
        _viewModel = viewModel;
        VerifyInstructionText = verifyInstructionText;

        LogOnViewModel.VerifyPasswordDialog.Show();

        VerifySignInPasswordDialog_Load();

        //while (DialogResult == DialogResult.None)
        //{
        //    Task.Delay(1000);
        //}

        bool result = DialogResult == DialogResult.OK;

        return result;
    }

    private void VerifySignInPasswordDialog_Load()
    {
        _viewModel.BindPropertyChanged(nameof(_viewModel.ShowPassword), (bool show) => { ShowPassphrase = show; });
    }

    public string ErrorMessage { get; set; }
    public string PassphraseText { get; set; }
    public bool ShowPassphrase { get; set; }
    public DialogResult DialogResult { get; set; }
    public string VerifyInstructionText { get; set; } = Texts.ChangeOptionGenericWarning;

    public void ButtonOk_Click(EventArgs e)
    {
        _viewModel.PasswordText = PassphraseText;
        _viewModel.ShowPassword = ShowPassphrase;

        if (!AdHocValidationDueToMonoLimitations())
        {
            DialogResult = DialogResult.None;
            return;
        }
        DialogResult = DialogResult.OK;
        VerifyInstructionText = "";
    }

    private bool AdHocValidationDueToMonoLimitations()
    {
        bool validated = AdHocValidatePassphrase();

        return validated;
    }

    private bool AdHocValidatePassphrase()
    {
        ErrorMessage = "";
        if (_viewModel[nameof(_viewModel.PasswordText)].Length != 0)
        {
            ErrorMessage = Texts.WrongPassphrase;
            return false;
        }
        return true;
    }

    private void PassphraseTextBox_Enter(EventArgs e)
    {
        ErrorMessage = "";
    }

    public void CancelButton_Click(EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        VerifyInstructionText = "";
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}
