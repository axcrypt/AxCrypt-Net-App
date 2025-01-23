using AxCrypt.Abstractions;
using AxCrypt.Content;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Services;

public class SupportService
{
    public async Task<bool> SendPremiumSupportRequestEmail(string subject, string message)
    {
        try
        {
            IAccountService accountService = New<LogOnIdentity, IAccountService>(New<KnownIdentities>().DefaultEncryptionIdentity);

            using (await New<IProgressDialog>().Show(Texts.ProgressIndicatorFeedbackMessage, Texts.ProgressIndicatorWaitMessage))
            {
                await accountService.PrioritySupportAsync(subject, message);
            }
            return true;
        }
        catch (Exception ex)
        {
            return false;
        }
    }
}