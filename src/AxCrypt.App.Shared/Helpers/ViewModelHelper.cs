using AxCrypt.Abstractions;
using AxCrypt.App.Shared.ViewModels;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Helpers;

public static class ViewModelHelper
{
    private static readonly int MaxShareUsersAllowedFree = 1;
    private static readonly int MaxShareUsersAllowedPremium = 10;
    private static readonly int MaxShareUsersAllowedBusiness = 20;

    public static IList<string> GetVisibilityTypeList()
    {
        return Enum.GetValues(typeof(SecretShareVisibility))
                .Cast<SecretShareVisibility>()
                .Where(vt => vt != SecretShareVisibility.None)
                .Select(v => v.ToString())
                .ToList();
    }

    public static int MaxAllowedUsersCountToShare()
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
            return MaxShareUsersAllowedFree;
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

    public static bool CanAddNewSecret()
    {
        if (New<AccountStatusViewModel>().PlanState == PlanState.HasPremium || New<AccountStatusViewModel>().PlanState == PlanState.HasPasswordManager || New<AccountStatusViewModel>().PlanState == PlanState.HasBusiness)
        {
            return true;
        }
        if (New<LogOnIdentity, AdditionalUserSettings>(New<KnownIdentities>().DefaultEncryptionIdentity).FreeUserSecretsCount < MaxAllowedSecretsCount)
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
        if (New<LogOnIdentity, AdditionalUserSettings>(New<KnownIdentities>().DefaultEncryptionIdentity).FreeUserSecretsCount < MaxAllowedSecretsCount)
        {
            return true;
        }
        return false;
    }
}