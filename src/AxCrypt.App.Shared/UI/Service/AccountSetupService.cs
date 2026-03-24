

using AxCrypt.App.Shared.Helpers;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;

namespace AxCrypt.App.Shared.UI.Services
{
    /// <summary>
    /// The account service. Methods and properties to work with an account.
    /// </summary>
    public class AccountSetupService : IAccountSetupService
    {
        public async Task CompleteAccountSetupAsync(LogOnIdentity logOnIdentity)
        {
            await AxCServiceProviderExtension.AccountSetupViewModel!.ShowAccountIncompleteWarningDialog(logOnIdentity);
        }
    }
}