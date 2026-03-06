using AxCrypt.Api.Model;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Extensions;
using System.Linq;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Core.Service
{
    public static class AccountServiceExtensions
    {
        public static async Task<bool> IsIdentityValidAsync(this IAccountService service)
        {
            if (service.Identity == LogOnIdentity.Empty)
            {
                return false;
            }

            UserAccount account = await service.AccountAsync().Free();
            if (account != null! && account.SubscriptionLevel == SubscriptionLevel.Unknown)
            {
                await New<IAccountSetupService>().CompleteAccountSetupAsync();
                return false;
            }

            if (!account.AccountKeys.Select(k => k.ToUserKeyPair(service.Identity.Passphrase)).Any((ukp) => ukp != null))
            {
                return false;
            }
            return true;
        }

        public static async Task<bool> IsAccountSourceLocalAsync(this IAccountService service)
        {
            UserAccount userAccount = await service.AccountAsync();

            return userAccount.AccountSource == AccountSource.Local;
        }
    }
}