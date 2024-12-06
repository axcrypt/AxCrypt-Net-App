using AxCrypt.App.Windows.Services;
using AxCrypt.App.Windows.ViewModels;
using AxCrypt.Core.UI;

namespace AxCrypt.App.Windows.Infrastructure.Dialogs;

public class VerifySignInPassword : VerifySignInPasswordBase
{
    protected override async Task<bool> VerifyDialog(string description)
    {
        VerifyPasswordViewModel verifyPasswordViewModel = AxCServiceProvider.GetService<VerifyPasswordViewModel>();
        bool result = await verifyPasswordViewModel.SetViewPassword(description);
        return result;
    }
}
