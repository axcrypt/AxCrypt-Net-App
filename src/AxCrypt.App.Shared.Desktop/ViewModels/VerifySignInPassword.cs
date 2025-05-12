using AxCrypt.App.Shared.Helpers;
using AxCrypt.Core.UI;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

public class VerifySignInPassword : VerifySignInPasswordBase
{
    protected override async Task<bool> VerifyDialog(string description)
    {
        VerifyPasswordViewModel verifyPasswordViewModel = AxCServiceProviderExtension.GetService<VerifyPasswordViewModel>();
        bool result = await verifyPasswordViewModel.SetViewPassword(description);
        return result;
    }
}
