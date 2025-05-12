using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.App.Shared.Utility;
using AxCrypt.Content;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class VerifyPasswordViewModel
{
    private VerifySignInPasswordViewModel _viewModel;

    private string? _verifyInstructionText;

    public LogOnViewModel LogOnViewModel { get; set; }

    public VerifyPasswordViewModel()
    {
        LogOnViewModel = AxCServiceProviderExtension.LogOnViewModel!;
        _viewModel = new VerifySignInPasswordViewModel(New<KnownIdentities>().DefaultEncryptionIdentity);
    }

    public async Task<bool> SetViewPassword(string verifyInstructionText)
    {
        VerifyInstructionText = verifyInstructionText;
        DialogResult = DialogResult.None;
        PassphraseText = "";
        _viewModel.BindPropertyChanged(nameof(_viewModel.ShowPassword), (bool show) => { ShowPassphrase = show; });

        LogOnViewModel.VerifyPasswordDialog.Show();

        while (DialogResult == DialogResult.None)
        {
            await Task.Delay(1000);
        }

        LogOnViewModel.VerifyPasswordDialog.Close();
        return DialogResult == DialogResult.OK;
    }

    public string? ErrorMessage { get; set; }
    public string? PassphraseText { get; set; }
    public bool ShowPassphrase { get; set; }
    public DialogResult DialogResult { get; set; }
    public string? VerifyInstructionText { get; set; }

    public void ButtonOk_Click(EventArgs e)
    {
        _viewModel.PasswordText = PassphraseText!;
        _viewModel.ShowPassword = ShowPassphrase;

        if (!AdHocValidationDueToMonoLimitations())
        {
            DialogResult = DialogResult.None;
            return;
        }
        DialogResult = DialogResult.OK;
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
        LogOnViewModel.VerifyPasswordDialog.Close();
    }

    public void ClearErrorProviders()
    {
        ErrorMessage = "";
    }
}