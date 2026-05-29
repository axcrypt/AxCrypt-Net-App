using System.Collections.Generic;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.Content;

namespace AxCrypt.App.Shared.Desktop.Models;

/// <summary>
/// One feature card inside a conversion dialog.
/// Icon is a raw SVG string rendered via MarkupString in the component.
/// </summary>
public sealed record ConversionDialogFeature(string Icon, string Title, string Desc);

/// <summary>
/// All copy and visual configuration for a single conversion dialog instance.
/// Build via the static factory methods — never construct directly.
///
/// Having all copy in one place means:
///   • A/B test copy changes touch exactly one file.
///   • Adding a new tier only requires a new factory method.
///   • The razor component (<see cref="ConversionDialog"/>) is copy-free.
/// </summary>
public sealed class ConversionDialogConfig
{
    public required string TierCssClass  { get; init; }
    public required string HeroBadge     { get; init; }
    public required string Headline      { get; init; }
    public required string Subhead       { get; init; }
    public required string CtaLabel      { get; init; }
    public required string RiskLine      { get; init; }
    public required IReadOnlyList<ConversionDialogFeature> Features    { get; init; }
    public required IReadOnlyList<string>                  TrustBadges { get; init; }

    // ── SVG icon snippets (shared across configs) ──────────────────────

    private const string IcoShield =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<path d='M12 2L4 6v6c0 5 3.5 10 8 11.35C16.5 22 20 17 20 12V6L12 2z'/></svg>";

    private const string IcoCloud =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<path d='M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z'/></svg>";

    private const string IcoKey =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<circle cx='9' cy='7' r='3'/><path d='M3 21v-2a4 4 0 014-4h4a4 4 0 014 4v2'/>" +
        "<path d='M17 11l2 2 4-4'/></svg>";

    private const string IcoLock =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<rect x='3' y='11' width='18' height='11' rx='2'/><path d='M7 11V7a5 5 0 0110 0v4'/></svg>";

    private const string IcoVault =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<rect x='2' y='3' width='20' height='14' rx='2'/><path d='M8 21h8'/><path d='M12 17v4'/></svg>";

    private const string IcoCompliance =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<path d='M12 2L4 6v6c0 5 3.5 10 8 11.35C16.5 22 20 17 20 12V6L12 2z'/>" +
        "<polyline points='9 12 11 14 15 10'/></svg>";

    private const string IcoGroup =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<circle cx='9' cy='7' r='3'/><path d='M3 21v-2a4 4 0 014-4h4a4 4 0 014 4v2'/>" +
        "<circle cx='17' cy='6' r='2'/><path d='M21 21v-2a4 4 0 00-3-3.86'/></svg>";

    private const string IcoAudit =
        "<svg width='20' height='20' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round'>" +
        "<path d='M22 12h-4l-3 9L9 3l-3 9H2'/></svg>";

    // ── Factory: first sign-in nudge (trial available) ────────────────

    public static ConversionDialogConfig ForTryNow(SignUpFrom signUpFrom)
    {
        bool isPremiumReturn = signUpFrom == SignUpFrom.Premium;
        return new ConversionDialogConfig
        {
            TierCssClass = isPremiumReturn ? "cnv-tier--premium" : "cnv-tier--generic",
            HeroBadge    = isPremiumReturn ? "Premium — reactivated free" : "14 days free",
            Headline     = isPremiumReturn ? "Welcome back. Premium on us." : "Get the full AxCrypt — on us.",
            Subhead      = isPremiumReturn
                ? "Your Premium features are one click away — no card needed."
                : "Join a million-plus users protecting files with AES-256. No card to start.",
            CtaLabel  = Texts.ButtonStartTrial,
            RiskLine  = "No payment to start • Cancel anytime",
            Features  = CommonConsumerFeatures(),
            TrustBadges = new[] { "AES-256", "HIPAA", "GDPR" },
        };
    }

    // ── Factory: subscription renewal / post-trial nudge ─────────────

    public static ConversionDialogConfig ForUpgrade(SignUpFrom signUpFrom)
    {
        bool isPremiumReturn = signUpFrom == SignUpFrom.Premium;
        return new ConversionDialogConfig
        {
            TierCssClass = isPremiumReturn ? "cnv-tier--premium" : "cnv-tier--generic",
            HeroBadge    = isPremiumReturn ? "Premium plan" : "Plans from $5/month",
            Headline     = isPremiumReturn ? "Pick up your Premium plan" : "Unlock the full AxCrypt",
            Subhead      = isPremiumReturn
                ? "Your encrypted files, vault and shared keys are waiting."
                : "Encryption, key sharing, secured vault and password manager — for the price of a coffee.",
            CtaLabel  = isPremiumReturn ? "Renew Premium" : Texts.UpgradePromptText,
            RiskLine  = "Monthly or yearly • Cancel anytime",
            Features  = isPremiumReturn ? PremiumRenewalFeatures() : CommonConsumerFeatures(),
            TrustBadges = new[] { "AES-256", "HIPAA", "GDPR" },
        };
    }

    // ── Factory: Business re-activation ──────────────────────────────

    public static ConversionDialogConfig ForBusiness() => new ConversionDialogConfig
    {
        TierCssClass = "cnv-tier--business",
        HeroBadge    = "For your whole team",
        Headline     = "Bring AxCrypt Business back to the team",
        Subhead      = "Restore admin controls, audit trail and key sharing across every seat — security your IT lead can sign off on.",
        CtaLabel     = "Renew Business plan",
        RiskLine     = "Per-seat pricing • Cancel anytime",
        Features     = BusinessFeatures(),
        TrustBadges  = new[] { "AES-256", "HIPAA", "NIS 2", "GDPR" },
    };

    // ── Shared feature-card lists ─────────────────────────────────────

    private static IReadOnlyList<ConversionDialogFeature> CommonConsumerFeatures() => new[]
    {
        new ConversionDialogFeature(IcoShield, "AES-256 everywhere",  Texts.TryUpgradePlanFeatureText1),
        new ConversionDialogFeature(IcoCloud,  "Cloud-ready",         Texts.TryUpgradePlanFeatureText2),
        new ConversionDialogFeature(IcoKey,    "Share keys safely",   Texts.HipaaBenefitsSubHeading2),
        new ConversionDialogFeature(IcoLock,   "Password Manager",    Texts.PricingFeatureListPasswordManagement),
    };

    private static IReadOnlyList<ConversionDialogFeature> PremiumRenewalFeatures() => new[]
    {
        new ConversionDialogFeature(IcoShield, "AES-256 everywhere",  Texts.TryUpgradePlanFeatureText1),
        new ConversionDialogFeature(IcoCloud,  "Cloud-ready",         Texts.TryUpgradePlanFeatureText2),
        new ConversionDialogFeature(IcoKey,    "Share keys safely",   Texts.HipaaBenefitsSubHeading2),
        new ConversionDialogFeature(IcoVault,  "Secured Vault",       "A dedicated, auto-encrypted folder for sensitive files."),
    };

    private static IReadOnlyList<ConversionDialogFeature> BusinessFeatures() => new[]
    {
        new ConversionDialogFeature(IcoCompliance, "Compliance-ready",     "HIPAA, GDPR, NIS 2 — audit-grade AES-256."),
        new ConversionDialogFeature(IcoGroup,      "Group management",     "Add seats, set roles, revoke keys instantly."),
        new ConversionDialogFeature(IcoLock,       "Master Key Recovery",  "Prevent data loss with organization-wide recovery access."),
        new ConversionDialogFeature(IcoAudit,      "Audit log",            "Every encrypt, share and access — timestamped."),
    };
}
