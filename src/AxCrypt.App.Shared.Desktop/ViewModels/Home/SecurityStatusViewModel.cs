using AxCrypt.Api.Model;
using AxCrypt.App.Shared.Desktop.Models;
using AxCrypt.App.Shared.Desktop.Services;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using AxCrypt.Content;
using AxCrypt.Core;
using AxCrypt.Core.UI.ViewModel;
using System.Collections.Generic;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.ViewModels.Home;

/// <summary>
/// ViewModel for the Security Status panel on the home page.
/// Owns tier detection, security item construction, and action routing.
/// </summary>
public class SecurityStatusViewModel : ViewModelBase
{
    private readonly UserService _userService;
    private readonly PaidFeaturegateService _paidGateService;

    private const string MfaSetupUrl = "https://account.axcrypt.net/en/Security#mfa-expnd-collps";

    public SecurityStatusViewModel(UserService userService, PaidFeaturegateService paidGateService)
    {
        _userService = userService;
        _paidGateService = paidGateService;

        // Re-derive items whenever the user's subscription changes.
        _userService.OnChange += UpdateViewState;
    }

    // ── Tier helpers ───────────────────────────────────────────
    public bool IsBusiness => _userService.SubscriptionLevel == SubscriptionLevel.Business;
    public bool IsPremium  => _userService.SubscriptionLevel == SubscriptionLevel.Premium
                           || _userService.SubscriptionLevel == SubscriptionLevel.PasswordManager;
    public bool IsFree     => _userService.IsFreeTier;

    public string TierLabel =>
        IsBusiness ? "Business" :
        IsPremium  ? "Premium"  : "Free";

    public string TierClass =>
        IsBusiness ? "business" :
        IsPremium  ? "premium"  : "free";

    // ── Security items ─────────────────────────────────────────
    /// <summary>Returns the full list of security rows for the current subscription tier.</summary>
    public List<SecurityItem> GetSecurityItems() => BuildItems(_userService.SubscriptionLevel);

    private static SecurityItem MfaItem()
    {
        // Derive MFA state from Core's account view model.
        bool isMfaEnabled = Core.UI.ViewModel.AxCryptUserAccountViewModel.MFAEnabledType != MultiFactorAuthType.None;
        return new()
        {
            Title       = Texts.MFAuthenticationLinkLable,
            Description = Texts.MFAuthenticationAdditionalFactorText,
            State       = isMfaEnabled ? SecurityItemState.Active : SecurityItemState.Warning,
            ActionRoute = MfaSetupUrl,
            ActionLabel = isMfaEnabled ? "" : "Set up",
        };
    }

    private static List<SecurityItem> BuildItems(SubscriptionLevel level)
    {
        bool business = level == SubscriptionLevel.Business;
        bool premium  = level == SubscriptionLevel.Premium || level == SubscriptionLevel.PasswordManager;

        if (business)
        {
            return new()
            {
                new() { Title = Texts.SecurityStatusUnlimitedAES256,   Description = Texts.SecurityStatusUnlimitedAES256Description,   State = SecurityItemState.Active },
                new() { Title = Texts.SecurityStatusMasterKeyRecovery, Description = Texts.SecurityStatusMasterKeyRecoveryDescription, State = SecurityItemState.Active },
                new() { Title = Texts.SecurityStatusGroupkeySharing,   Description = Texts.SecurityStatusGroupkeySharingDescription,   State = SecurityItemState.Active },
                MfaItem(),
            };
        }

        if (premium)
        {
            return new()
            {
                new() { Title = Texts.SecurityStatusUnlimitedAES256,  Description = Texts.SecurityStatusUnlimitedDescription,  State = SecurityItemState.Active },
                new() { Title = Texts.UsageTipsAnonymousFileHeading,  Description = Texts.UsageTipsAnonymousFileDescription,   State = SecurityItemState.Active },
                MfaItem(),
            };
        }

        // Free tier
        return new()
        {
            new() { Title = Texts.SecurityStatusEncryptionsPerMonth, Description = Texts.SecurityStatusFreePlan,               State = SecurityItemState.Active },
            new() { Title = Texts.SecurityStatusUnlimitedAES256,     Description = Texts.SecurityStatusUnlimitedDescription1, State = SecurityItemState.Locked, ActionRoute = "/upgradePage", ActionLabel = "Upgrade" },
            new() { Title = Texts.SecureDeleteToolStripMenuItemText, Description = Texts.SecurityStatusSecureDeleteTitle,      State = SecurityItemState.Locked, ActionRoute = "/upgradePage", ActionLabel = "Upgrade" },
        };
    }

    // ── Action routing ─────────────────────────────────────────
    /// <summary>
    /// Returns the route string for the given security item's action,
    /// or null if there is no action. The razor calls NavigateTo / BrowseUtility
    /// based on whether the route is an http URL or an in-app path.
    /// </summary>
    public string? GetActionRoute(SecurityItem item) => item.ActionRoute;

    /// <summary>Returns true when the action route is an external URL (http/https).</summary>
    public static bool IsExternalUrl(string? route) =>
        !string.IsNullOrEmpty(route) && route.StartsWith("http", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>Opens an external action URL via BrowseUtility (Core concern, not razor).</summary>
    public static void OpenExternalUrl(string url) => BrowseUtility.RedirectTo(url);
}
