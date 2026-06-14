using System;
using System.Collections.Generic;
using System.Linq;

namespace AxCrypt.App.Shared.Desktop.ViewModels;

/// <summary>
/// Singleton ViewModel for the OTP-based sign-up flow.
/// Holds all form state so it survives navigation from SignUpPage → PlanAndPayPage
/// and so PlanAndPayPage can pre-populate / pass business info to checkout.
/// </summary>
public class SignUpViewModel
{
    // ── Step 1 ─────────────────────────────────────────────────────────────
    public string Email { get; set; } = string.Empty;
    public bool IsBusinessAccount { get; set; } = false;
    public bool BusinessEmailSuggested { get; set; } = false;

    // Business fields (shown when IsBusinessAccount = true)
    public string OrganizationName { get; set; } = string.Empty;
    public string Country { get; set; } = "SE";
    public string VatNumber { get; set; } = string.Empty;

    // ── Step 2 ─────────────────────────────────────────────────────────────
    public string[] OtpDigits { get; set; } = new string[6];
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool ShowPassword { get; set; } = false;
    public bool ShowConfirmPassword { get; set; } = false;

    // ── Computed helpers ────────────────────────────────────────────────────
    public bool IsEuCountry => _euCountries.Contains(Country);
    public string OtpCode => string.Concat(OtpDigits);
    public bool AllOtpFilled => OtpDigits.All(d => !string.IsNullOrEmpty(d));

    // True when the user arrived at PlanAndPay from a fresh signup session
    public bool IsFromSignup => !string.IsNullOrEmpty(Email);

    // ── Password strength ───────────────────────────────────────────────────
    public bool PwHasLength   => Password.Length >= 8;
    public bool PwHasCase     => Password.Any(char.IsUpper) && Password.Any(char.IsLower);
    public bool PwHasNumber   => Password.Any(char.IsDigit);
    public bool PwHasSymbol   => Password.Any(c => !char.IsLetterOrDigit(c));
    public bool PwConfirmMatch => Password.Length > 0 && Password == ConfirmPassword;

    public int PwRulesPassed =>
        (PwHasLength ? 1 : 0) + (PwHasCase ? 1 : 0) +
        (PwHasNumber ? 1 : 0) + (PwHasSymbol ? 1 : 0);

    public string PwStrengthLabel => PwRulesPassed switch
    {
        0 => "", 1 => "Weak", 2 => "Fair", 3 => "Good", _ => "Strong"
    };

    public string PwStrengthColor => PwRulesPassed switch
    {
        0 => "#d1d5db", 1 => "#ef4444", 2 => "#f59e0b", 3 => "#84cc16", _ => "#22c55e"
    };

    public int PwStrengthPct => PwRulesPassed * 25;

    // ── Email helpers ───────────────────────────────────────────────────────
    public bool IsFreeEmailDomain(string domain) => _freeEmailDomains.Contains(domain);

    // ── Lifecycle ───────────────────────────────────────────────────────────
    /// <summary>
    /// Full reset — called by SignUpPage.OnInitialized on each fresh visit.
    /// Does NOT wipe OrganizationName / Country / VatNumber so PlanAndPayPage
    /// can still read them when navigating back or re-initialising.
    /// </summary>
    public void ResetForNewSession()
    {
        Email = string.Empty;
        IsBusinessAccount = false;
        BusinessEmailSuggested = false;
        OrganizationName = string.Empty;
        Country = "SE";
        VatNumber = string.Empty;
        ResetStep2();
    }

    /// <summary>Clears OTP + password fields only (e.g. after a failed OTP).</summary>
    public void ResetStep2()
    {
        OtpDigits = new string[6];
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        ShowPassword = false;
        ShowConfirmPassword = false;
    }

    // ── Static data ─────────────────────────────────────────────────────────
    private static readonly HashSet<string> _euCountries =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "AT","BE","BG","CY","CZ","DE","DK","EE","ES","FI",
            "FR","GR","HR","HU","IE","IT","LT","LU","LV","MT",
            "NL","PL","PT","RO","SE","SI","SK"
        };

    private static readonly HashSet<string> _freeEmailDomains =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com","yahoo.com","hotmail.com","outlook.com","live.com",
            "icloud.com","me.com","aol.com","protonmail.com","proton.me",
            "mail.com","yandex.com","tutanota.com","zoho.com","msn.com",
            "googlemail.com","yahoo.co.uk","yahoo.co.in","rediffmail.com",
            "gmx.com","gmx.de","web.de","qq.com","163.com","126.com"
        };
}
