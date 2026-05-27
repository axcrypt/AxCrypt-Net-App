using AxCrypt.Api;
using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Utility;

public static class SecMessengerUtility
{
    private static readonly int MaxSendUsersAllowedFree = 1;
    private static readonly int MaxSendUsersAllowedPremium = 10;
    private static readonly int MaxSendUsersAllowedBusiness = 20;
    private static IFeatureUsageProvider? usage;

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

        usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
        if (usage.GetUsage(FeatureKey.SecuredMessage).Remaining > 0)
        {
            return true;
        }

        return false;
    }

    public static bool CanUpdateFreeUserCount(SubscriptionLevel subscriptionLevel)
    {
        if (New<LicensePolicy>().Capabilities.Has(LicenseCapability.SendUnlimitedMessages))
        {
            return false;
        }

        usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
        if (usage.GetUsage(FeatureKey.SecuredMessage).Remaining > 0)
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

    public static int MaxReceiversToDisplay = 2;

    public static string ToDateString(DateTime dateTime)
    {
        DateTime utcNow = New<Abstractions.INow>().Utc;
        if (dateTime.Year != utcNow.Year)
        {
            return dateTime.ToString("dd/MMM/yyyy");
        }

        if (dateTime.Month != utcNow.Month)
        {
            return dateTime.ToString("dd/MMM");
        }

        if (dateTime.AddDays(7) < utcNow.AddDays(-7))
        {
            return dateTime.ToString("dd/MMM");
        }

        return dateTime.ToString("ddd hh:mm tt");
    }

    public static bool IsBusinessUser
    {
        get
        {
            //    if (IsUserAuthorized())
            //    {
            //        return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.Business;
            //    }
            return false;
        }
    }

    public static bool IsPremiumUser
    {
        get
        {
            //if (IsUserAuthorized())
            {
                //return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.Premium;
            }
            return false;
        }
    }

    public static bool IsPasswordManager
    {
        get
        {
            //if (IsUserAuthorized())
            //{
            //    return New<IXecretsUserGateway>(UserContext.Name).UserSubscriptionLevel == SubscriptionLevel.PasswordManager;
            //}
            return false;
        }
    }
}