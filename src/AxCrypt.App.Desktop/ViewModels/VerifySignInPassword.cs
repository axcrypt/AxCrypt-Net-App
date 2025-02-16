using AxCrypt.App.Desktop.Services;
using AxCrypt.Core.UI;
using System.Threading.Tasks;

namespace AxCrypt.App.Desktop.ViewModels;

public class VerifySignInPassword : VerifySignInPasswordBase
{
    protected override async Task<bool> VerifyDialog(string description)
    {
        VerifyPasswordViewModel verifyPasswordViewModel = AxCServiceProvider.GetService<VerifyPasswordViewModel>();
        bool result = await verifyPasswordViewModel.SetViewPassword(description);
        return result;
    }
}
