using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Shared.Services;
using AxCrypt.Core;
using AxCrypt.Core.Crypto;
using AxCrypt.Core.Service;
using AxCrypt.Core.UI;
using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Shared.Desktop.Services;

/// <summary>
/// Drives the new three-step online sign-up flow:
///   1. Email entered → server emails a 6-digit one-time code.
///   2. User pastes the code → server returns a short-lived verification
///      token that lets the account-web flow trust the email is owned by
///      the user.
///   3. Password set locally → we redirect to the account web for plan
///      selection, carrying email + verification token as query params
///      so the web flow can skip its own email-verification step and
///      go straight to the plan grid.
///
/// This service is intentionally separate from the legacy
/// <c>RegisterViewModel</c> flow — the new flow is OTP-first, the legacy
/// flow created a local key pair immediately. The two have different
/// failure modes and a single dispatch path would tangle them.
///
/// The API calls (SendCode / VerifyCode) are deliberately abstracted —
/// the existing <c>AccountService</c> doesn't expose an OTP endpoint
/// yet, so this service stubs them with a deterministic mock that the
/// integration tests use. Wiring to the real endpoints is a one-line
/// swap inside <see cref="SendCodeAsync"/> and
/// <see cref="VerifyCodeAsync"/>.
/// </summary>
public class OnlineSignUpService
{
    /// <summary>Current step in the flow. The UI watches this to swap panes.</summary>
    public SignUpStep Step { get; private set; } = SignUpStep.Email;

    /// <summary>Email captured at step 1.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>OTP code entered at step 2.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Password captured at step 3, only used to log the user in locally
    /// after they return from the account-web payment step.</summary>
    public string Password { get; private set; } = string.Empty;

    /// <summary>
    /// Server-issued token returned by <see cref="VerifyCodeAsync"/>. Passed
    /// along to the account-web hand-off URL so the web flow doesn't
    /// re-verify the email. Empty until step 2 succeeds.
    /// </summary>
    public string VerificationToken { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when the latest code was sent. Used for the
    /// "Resend in N seconds" cooldown UI.</summary>
    public DateTime? CodeSentAtUtc { get; private set; }

    /// <summary>Seconds the user has to wait before requesting a fresh code.</summary>
    public int ResendCooldownSeconds { get; private set; } = 30;

    /// <summary>True while an async call is in flight. UI buttons gate on this.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>Friendly error message from the latest failed call.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Raised whenever any property above changes. The Razor page
    /// hooks this to <c>StateHasChanged</c>.</summary>
    public event Action? OnChange;

    private void Notify() => OnChange?.Invoke();

    /// <summary>
    /// Step back from Password → Code without wiping the verification
    /// token. Used by the Back button on the password step.
    /// </summary>
    public void StepBackToCode()
    {
        if (Step == SignUpStep.Password)
        {
            Step = SignUpStep.Code;
            ErrorMessage = null;
            Notify();
        }
    }

    /// <summary>Reset the flow back to step 1. Called when the user
    /// cancels or after a successful plan-step redirect.</summary>
    public void Reset()
    {
        Step = SignUpStep.Email;
        Email = string.Empty;
        Code = string.Empty;
        Password = string.Empty;
        VerificationToken = string.Empty;
        CodeSentAtUtc = null;
        IsBusy = false;
        ErrorMessage = null;
        Notify();
    }

    // ── Step 1 ─────────────────────────────────────────────────────────

    /// <summary>
    /// Validate the email shape locally, then ask the server to email an
    /// OTP. Advances <see cref="Step"/> to <see cref="SignUpStep.Code"/>
    /// on success.
    /// </summary>
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

            await New<LogOnIdentity, IAccountService>(New<KnownIdentities>().DefaultEncryptionIdentity).SignupAsync(EmailAddress.Parse(email!), new CultureInfo(Resolve.UserSettings.CultureName), "");

            await Task.Delay(450); // simulate network round-trip

            CodeSentAtUtc = DateTime.UtcNow;
            Step = SignUpStep.Code;
            if (isBusiness != null)
            {
                SignUpFrom signUpFrom = isBusiness.Value ? SignUpFrom.Business : SignUpFrom.Premium;
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

    /// <summary>
    /// Resend the OTP if the cooldown has elapsed. Returns false if the
    /// user is still inside the cooldown window.
    /// </summary>
    public async Task<bool> ResendCodeAsync()
    {
        if (!CanResend())
        {
            return false;
        }
        // Re-issue against the same email; do NOT advance Step.
        return await SendCodeAsync(Email);
    }

    /// <summary>True if enough time has passed since the last send for a resend.</summary>
    public bool CanResend()
    {
        if (CodeSentAtUtc == null) return true;
        return (DateTime.UtcNow - CodeSentAtUtc.Value).TotalSeconds >= ResendCooldownSeconds;
    }

    /// <summary>Seconds left on the cooldown, clamped to 0..ResendCooldownSeconds.</summary>
    public int SecondsUntilResend()
    {
        if (CodeSentAtUtc == null) return 0;
        double remaining = ResendCooldownSeconds - (DateTime.UtcNow - CodeSentAtUtc.Value).TotalSeconds;
        return remaining <= 0 ? 0 : (int)Math.Ceiling(remaining);
    }

    // ── Step 3 ─────────────────────────────────────────────────────────

    /// <summary>
    /// Verify the OTP against the server. On success, captures the
    /// short-lived <see cref="VerificationToken"/> and advances
    /// <see cref="Step"/> to <see cref="SignUpStep.Password"/>.
    /// </summary>
    public async Task<bool> VerifyCodeAsync(string code)
    {
        ErrorMessage = null;

        string trimmed = (code ?? string.Empty).Trim();
        // The OTP is six digits, no spaces. Anything else fails locally
        // before we round-trip to the server.
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

            // Mock a server-issued token. Real backend returns a JWT or
            // a one-time hash that the account web validates.
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

    // ── Step 2 ─────────────────────────────────────────────────────────

    /// <summary>
    /// Capture the password locally and mark the in-app step done. The
    /// UI then hands off to the account web with email + token. We do
    /// NOT post the password to the web — the web won't ask for it
    /// because the user will sign back in locally once they return.
    /// </summary>
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
    /// Build the account-web hand-off URL with email + verification
    /// token + Premium-trial prefill so the web lands on the right plan
    /// tile. Returns the URL template suitable for
    /// <c>BrowseUtility.RedirectToAccountWebUrl</c> (i.e. the leading
    /// "{0}" base placeholder is preserved).
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

/// <summary>Linear three-step state machine for the new sign-up flow.</summary>
public enum SignUpStep
{
    Email,
    Code,
    Password,
    Done,
}
