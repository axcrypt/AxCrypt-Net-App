using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Services;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using Microsoft.Maui.Devices;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Three-step OTP sign-up: email → 6-digit code → password → plan hand-off.
/// Separate from RegisterViewModel (OTP-first vs. local key pair).
/// API calls stub through AccountService; swap the body when OTP endpoints land.
/// </summary>
public class OnlineSignUpService
{
    public SignUpStep Step { get; private set; } = SignUpStep.Email;
    public string Email { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    /// <summary>Server-issued token for the account-web hand-off.</summary>
    public string VerificationToken { get; private set; } = string.Empty;

    // ── Business info captured in Step 1 ──────────────────────────────
    public string OrganizationName { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string VatNumber { get; private set; } = string.Empty;

    public void SetBusinessInfo(string orgName, string country, string vat)
    {
        OrganizationName = orgName ?? string.Empty;
        Country = country ?? string.Empty;
        VatNumber = vat ?? string.Empty;
    }

    public DateTime? CodeSentAtUtc { get; private set; }
    public int ResendCooldownSeconds { get; private set; } = 30;
    public bool IsBusy { get; private set; }
    public bool EFF { get; set; }
    public string? ErrorMessage { get; private set; }

    /// <summary>Raised on any state change — UI calls StateHasChanged.</summary>
    public event Action? OnChange;

    private void Notify() => OnChange?.Invoke();

    /// <summary>Password → Code without dropping the verification token.</summary>
    public void StepBackToCode()
    {
        if (Step == SignUpStep.Password)
        {
            Step = SignUpStep.Code;
            ErrorMessage = null;
            Notify();
        }
    }

    public void Reset()
    {
        Step = SignUpStep.Email;
        Email = string.Empty;
        Code = string.Empty;
        Password = string.Empty;
        VerificationToken = string.Empty;
        OrganizationName = string.Empty;
        Country = string.Empty;
        VatNumber = string.Empty;
        CodeSentAtUtc = null;
        IsBusy = false;
        ErrorMessage = null;
        Notify();
    }

    // ── Step 1 ─────────────────────────────────────────────────────────

    /// <summary>Send the OTP. Advances Step → Code on success.</summary>
    public async Task<bool> SendCodeAsync(string email, bool? isBusiness = null)
    {
        ErrorMessage = null;

        string trimmed = email?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(trimmed) || !LooksLikeEmail(trimmed))
        {
            ErrorMessage = "Please enter a valid email address.";
            Notify();
            return false;
        }

        Email = trimmed;
        IsBusy = true;
        Notify();

        try
        {
            // Private signup → Free so PlanAndPay shows both Free + Premium cards
            SignUpFrom signUpFrom = isBusiness == null ? SignUpFrom.Premium : isBusiness.Value ? SignUpFrom.Business : SignUpFrom.Premium;
            string platForm = GetPlatForm();

            await New<LogOnIdentity, IAccountService>(New<KnownIdentities>().DefaultEncryptionIdentity).SignupAsync(EmailAddress.Parse(email!), new CultureInfo(Resolve.UserSettings.CultureName), signUpFrom.ToString(), "", platForm);

            await Task.Delay(450); // stubbed network

            CodeSentAtUtc = DateTime.UtcNow;
            Step = SignUpStep.Code;
            if (isBusiness != null)
            {
                //SignUpFrom signUpFrom = isBusiness.Value ? SignUpFrom.Business : SignUpFrom.Premium;
                AxCServiceProvider.GetService<UserService>().InitializeData(SubscriptionLevel.Unknown, email!, signUpFrom);
            }
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Couldn't send the code. {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            Notify();
        }
    }

    private static string GetPlatForm()
    {
        if (DeviceInfo.Platform == DevicePlatform.WinUI)
        {
            return "Windows";
        }

        if (DeviceInfo.Platform == DevicePlatform.MacCatalyst || DeviceInfo.Platform == DevicePlatform.macOS)
        {
            return "Mac";
        }

        return "";
    }

    /// <summary>Resend if the cooldown has elapsed.</summary>
    public async Task<bool> ResendCodeAsync()
    {
        if (!CanResend())
        {
            return false;
        }
        return await SendCodeAsync(Email);
    }

    public bool CanResend()
    {
        if (CodeSentAtUtc == null) return true;
        return (DateTime.UtcNow - CodeSentAtUtc.Value).TotalSeconds >= ResendCooldownSeconds;
    }

    public int SecondsUntilResend()
    {
        if (CodeSentAtUtc == null) return 0;
        double remaining = ResendCooldownSeconds - (DateTime.UtcNow - CodeSentAtUtc.Value).TotalSeconds;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }

    // ── Step 2 ─────────────────────────────────────────────────────────

    /// <summary>Verify the OTP. Captures the token and advances Step → Password.</summary>
    public async Task<bool> VerifyCodeAsync(string code)
    {
        ErrorMessage = null;

        string trimmed = (code ?? string.Empty).Trim();
        // Fail-fast locally: OTP is six digits exactly.
        if (trimmed.Length != 6 || !AllDigits(trimmed))
        {
            ErrorMessage = "Enter the 6-digit code from your email.";
            Notify();
            return false;
        }

        Code = trimmed;
        IsBusy = true;
        Notify();

        try
        {
            LogOnIdentity logOnIdentity = new LogOnIdentity(EmailAddress.Parse(Email!), new Passphrase(Password));
            await New<LogOnIdentity, IAccountService>(logOnIdentity).PasswordResetAsync(code!);

            await Task.Delay(550);

            // Stubbed token — real backend returns a JWT / one-time hash.
            VerificationToken = Guid.NewGuid().ToString("N");
            Step = SignUpStep.Done;
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"That code didn't match. {ex.Message}";
            return false;
        }
        finally
        {
            IsBusy = false;
            Notify();
        }
    }

    // ── Step 3 ─────────────────────────────────────────────────────────

    /// <summary>Capture the password locally; web hand-off uses email + token only.</summary>
    public bool CapturePassword(string password, string verify)
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            ErrorMessage = "Use a password with at least 8 characters.";
            Notify();
            return false;
        }
        if (!string.Equals(password, verify, StringComparison.Ordinal))
        {
            ErrorMessage = "Passwords don't match — please retype.";
            Notify();
            return false;
        }

        Password = password;
        Step = SignUpStep.Password;
        Notify();
        return true;
    }

    /// <summary>
    /// Hand-off URL template for BrowseUtility.RedirectToAccountWebUrl —
    /// leaves the leading "{0}" base placeholder intact.
    /// </summary>
    public string BuildPlanRedirectUrl(bool startWithTrial)
    {
        string email = Uri.EscapeDataString(Email);
        string token = Uri.EscapeDataString(VerificationToken);
        string plan = startWithTrial ? "&reqFrom=Premium&trial=1" : "";
        return $"{{0}}HomeUser/Login?Signup=True&email={email}&token={token}{plan}";
    }

    // ── Helpers ────────────────────────────────────────────────────────

    private static bool LooksLikeEmail(string s)
    {
        int at = s.IndexOf('@');
        return at > 0 && at < s.Length - 1 && s.IndexOf('.', at) > at;
    }

    private static bool AllDigits(string s)
    {
        foreach (char c in s)
        {
            if (c < '0' || c > '9') return false;
        }
        return true;
    }
}

public enum SignUpStep
{
    Email,
    Code,
    Password,
    Done,
}
