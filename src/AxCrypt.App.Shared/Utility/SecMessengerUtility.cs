using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.Api.Model.SecuredMessenger;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.Service.SecuredMessenger;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility;

public static class SecMessengerUtility
{
    private static readonly int MaxSendUsersAllowedFree = 1;
    private static readonly int MaxSendUsersAllowedPremium = 10;
    private static readonly int MaxSendUsersAllowedBusiness = 20;

    public static int MaxSendUserCount(SubscriptionLevel subscriptionLevel)
    {
        if (subscriptionLevel == SubscriptionLevel.Free)
        {
            return MaxSendUsersAllowedFree;
        }

        if (subscriptionLevel == SubscriptionLevel.PasswordManager)
        {
            return MaxSendUsersAllowedFree;
        }

        if (subscriptionLevel == SubscriptionLevel.Premium)
        {
            return MaxSendUsersAllowedPremium;
        }

        if (subscriptionLevel == SubscriptionLevel.Business)
        {
            return MaxSendUsersAllowedBusiness;
        }

        throw new InvalidOperationException("Invalid subscription level to find the maximum users count for send message!");
    }

    public static readonly int MaxMessageCreationAllowed = 10;

    public static bool AllowAddNewMessage(SubscriptionLevel subscriptionLevel)
    {
        if (New<LicensePolicy>().Capabilities.Has(LicenseCapability.SendUnlimitedMessages))
        {
            return true;
        }

        IEnumerable<SecuredMessengerApiModel> messages = new List<SecuredMessengerApiModel>();
        Task.Run(async () =>
        {
            AxCrypt.Core.Crypto.LogOnIdentity identity = New<KnownIdentities>().DefaultEncryptionIdentity;
            messages = await New<LogOnIdentity, ISecuredMessengerService>(identity).GetSentListAsync(GetRequestOptions());
        }).Wait();

        if (messages.Count() < MaxMessageCreationAllowed)
        {
            return true;
        }

        return false;
    }

    public static readonly int SecuredMessagePageCount = 10;

    public static RequestOptions GetRequestOptions()
    {
        return new RequestOptions()
        {
            UserName = New<KnownIdentities>().DefaultEncryptionIdentity.UserEmail.Address,
            PageCount = SecuredMessagePageCount
        };
    }
}