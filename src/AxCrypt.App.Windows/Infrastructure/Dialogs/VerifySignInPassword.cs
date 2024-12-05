using AxCrypt.App.Components.Models;
using AxCrypt.App.Components.Services;
using AxCrypt.App.Components.Utility;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;

using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Windows.Infrastructure.Dialogs;

public class VerifySignInPassword : VerifySignInPasswordBase
{
    //private Page _parent;
    private VerifyPasswordViewModel? _verifyPasswordViewModel;

    //public VerifySignInPassword(Page parent)
    //{
    //    _parent = parent;
    //}

    protected override bool VerifyDialog(string description)
    {
        ProcessIndicatorService processIndicatorService = new ProcessIndicatorService();
        _verifyPasswordViewModel = new VerifyPasswordViewModel();
        VerifySignInPasswordViewModel viewModel = new VerifySignInPasswordViewModel(New<KnownIdentities>().DefaultEncryptionIdentity);
        _verifyPasswordViewModel.SetViewPassword(viewModel,description);
        //_logOnViewModel.VerifyPasswordDialog.Show();

        //while (_verifyPasswordViewModel.DialogResult == DialogResult.None)
        //{
        //    Task.Delay(1000);
        //}

        return false;
        //trigger verify password dialog
        //return _parent.ShowVerifySignInPasswordDialog(description);
    }
}
