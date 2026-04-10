// ═══════════════════════════════════════════
//  PAID FEATURE GATE
//  Surfaces a feature-targeted upgrade popup. Tier-aware:
//
//    • SignUpFrom == None / Free     → generic popup
//    • SignUpFrom == Premium         → "Upgrade to Premium"   (premium brand)
//    • SignUpFrom == Business        → "Upgrade to Business"  (business brand)
//
//  The popup's CTA always navigates to /upgradePage, which then
//  shows the appropriate plan grid based on the same SignUpFrom value.
// ═══════════════════════════════════════════

using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Helpers;
using AxCrypt.App.Shared.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AxCrypt.App.Shared.Desktop.Services;

public class PaidFeaturegateService
{
    PopupService _popupService;
    UserService _userService;

    public PaidFeaturegateService()
    {
        _popupService = AxCServiceProviderExtension.GetService<PopupService>();
        _userService = AxCServiceProviderExtension.GetService<UserService>();
    }

    public string PaidGateFeature { get; set; } = string.Empty;
    public string PaidGateDescription { get; set; } = string.Empty;
    public string[] PaidGatePerks { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Plan we're pitching in the popup. Derived from the signed-in user's
    /// <see cref="UserService.SignUpFrom"/>, but exposed so consumers can
    /// inspect it (e.g. the popup picks the headline + brand color from it).
    /// </summary>
    public SignUpFrom TargetTier { get; private set; } = SignUpFrom.None;

    /// <summary>Headline shown at the top of the popup.</summary>
    public string TargetHeadline =>
        TargetTier switch
        {
            SignUpFrom.Premium  => "{0} is a Premium feature",
            SignUpFrom.Business => "{0} is a Business feature",
            _                   => "{0} is a paid feature",
        };

    /// <summary>CTA label on the upgrade button.</summary>
    public string TargetCtaLabel =>
        TargetTier switch
        {
            SignUpFrom.Premium  => "Upgrade to Premium",
            SignUpFrom.Business => "Upgrade to Business",
            _                   => "Upgrade",
        };

    /// <summary>
    /// CSS-friendly class suffix the popup applies to its container, so
    /// the brand color follows the tier:
    ///   pgm-tier--premium  → green gradient
    ///   pgm-tier--business → dark / SOC color
    ///   pgm-tier--generic  → default amber
    /// </summary>
    public string TargetCssClass =>
        TargetTier switch
        {
            SignUpFrom.Premium  => "pgm-tier--premium",
            SignUpFrom.Business => "pgm-tier--business",
            _                   => "pgm-tier--generic",
        };

    public bool IsPaidFeature(string key) =>
        _userService.IsFreeTier && new[] { "share", "vault", "cloud", "folder" }.Contains(key);

    public void ShowPaidGate(string feature, string desc, string[] perks)
    {
        PaidGateFeature = feature;
        PaidGateDescription = desc;
        PaidGatePerks = perks;
        TargetTier = ResolveTargetTier();
        _popupService.ShowPaidGate();
    }

    /// <summary>
    /// Map the user's SignUpFrom to a popup target tier.
    /// • None / Free → generic (we'll show the full /upgradePage ladder).
    /// • Premium     → pitch Premium re-activation.
    /// • Business    → pitch Business re-activation.
    /// </summary>
    private SignUpFrom ResolveTargetTier()
    {
        return _userService.SignUpFrom switch
        {
            SignUpFrom.Premium  => SignUpFrom.Premium,
            SignUpFrom.Business => SignUpFrom.Business,
            _                   => SignUpFrom.None,
        };
    }
}
