using AxCrypt.Abstractions;
using AxCrypt.App.Desktop.ViewModels;
using AxCrypt.Common;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Runtime;
using AxCrypt.Core.UI;
using AxCrypt.Core.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Desktop.Helpers;

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
        PlanState planState = New<AccountStatusViewModel>().PlanState;
        switch (planState)
        {
            case PlanState.NoPremium:
                return MaxShareUsersAllowedFree;

            case PlanState.HasPasswordManager:
                return MaxShareUsersAllowedPremium;

            case PlanState.HasPremium:
                return MaxShareUsersAllowedPremium;

            case PlanState.HasBusiness:
                return MaxShareUsersAllowedBusiness;

            default:
                throw new ArgumentException("Invalid subscription level.", planState.ToString());
        }
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