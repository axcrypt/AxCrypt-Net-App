using AxCrypt.Abstractions;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Entitlement.Services;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers;

public static class ViewModelHelper
{
    private static readonly int MaxShareUsersAllowedPremium = 10;
    private static readonly int MaxShareUsersAllowedBusiness = 20;

    private static IFeatureUsageProvider usage;

    static ViewModelHelper()
    {
        usage = AxCServiceProviderExtension.GetService<IFeatureUsageProvider>();
    }

    public static IList<string> GetVisibilityTypeList()
    {
        return Enum.GetValues(typeof(SecretShareVisibility))
                .Cast<SecretShareVisibility>()
                .Where(vt => vt != SecretShareVisibility.None)
                .Select(v => v.ToString())
                .ToList();
    }

    public static async Task<int> MaxAllowedUsersCountToShare()
    {
        LicenseCapabilities Capability = New<LicensePolicy>().Capabilities;

        if (Capability.Has(LicenseCapability.Business))
        {
            return MaxShareUsersAllowedBusiness;
        }

        if (Capability.Has(LicenseCapability.ShareSecretPremium) || Capability.Has(LicenseCapability.ShareSecretPasswordManager))
        {
            return MaxShareUsersAllowedPremium;
        }

        if (Capability.Has(LicenseCapability.ShareSecretFree))
        {
            return await New<UserEntitlementService>().GetRemainingCount(LimitedCapability.ShareSecret, New<AccountStatusViewModel>().SubscriptionLevel);
        }

        return 0;
    }

    public static bool IsAxCryptOnline()
    {
        if (New<AxCryptOnlineState>().IsOffline)
        {
            return false;
        }
        return New<IInternetState>().Connected;
    }

    public static readonly int MaxAllowedSecretsCount = 10;

    private static long freeUserCount;

    public static async Task<bool> CheckFreeUserSecretsCountasync()
    {
        freeUserCount = usage.GetUsage(FeatureKey.PasswordEntry).Remaining;
        return freeUserCount > 0;
    }

    public static bool CanAddNewSecret()
    {
        if (New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness)
        {
            return true;
        }
        if (freeUserCount > 0)
        {
            return true;
        }
        return false;
    }

    public static bool CanUpdateFreeUserNewSecretCount()
    {
        if (New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness)
        {
            return false;
        }
        if (freeUserCount > 0)
        {
            return true;
        }
        return false;
    }
}