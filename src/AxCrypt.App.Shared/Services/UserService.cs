using System;
using System.Linq;
using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.Core.UI.ViewModel;

namespace AxCrypt.App.Shared.Services;

public class UserService
{
    public event Action? OnChange;

    private void Notify() => OnChange?.Invoke();

    // ── Account ────────────────────────────────────────────────
    public string UserEmail { get; private set; } = "";

    public SubscriptionLevel SubscriptionLevel { get; set; } = SubscriptionLevel.Free;

    private SignUpFrom _signUpFrom = SignUpFrom.Free;

    public SignUpFrom SignUpFrom
    {
        get
        {
            return _signUpFrom;
        }
    }

    /// <summary>
    /// True for Free (and unauthenticated) users. This is the authoritative,
    /// synchronously-set tier gate the FreePlanLimitBar uses to decide
    /// whether to render at all.
    ///
    /// Per-feature usage counts and caps are NOT tracked here — they live
    /// in <c>AxCrypt.App.Entitlement.Contracts.IFeatureUsageProvider</c>,
    /// which is API-backed (with an offline cache). UserService used to
    /// carry hardcoded EncryptedCount / FreeLimit fields; those were a
    /// misleading second source of truth and have been removed.
    /// </summary>
    public bool IsFreeTier { get; private set; } = false;

    // ── Derived display fields ─────────────────────────────────
    public string DisplayName => BuildDisplayName(UserEmail);
    public string UserInitials() { return BuildInitials(UserEmail); }
    public string UserAvatarColor() { return BuildAvatarColor(UserEmail); }
    public string PlanLabel => IsFreeTier ? "Free Plan" : "Premium Plan";

    public void InitializeData(Api.Model.SubscriptionLevel subscriptionLevel, string userEmail)
    {
        SignUpFrom signUpFrom =
            Enum.TryParse(AxCryptUserAccountViewModel.SignUpFrom, ignoreCase: true, out SignUpFrom parsed)
                ? parsed
                : SignUpFrom.None;

        InitializeData(subscriptionLevel, userEmail, signUpFrom);
    }

    public void InitializeData(Api.Model.SubscriptionLevel subscriptionLevel, string userEmail, SignUpFrom signUpFrom)
    {
        UserEmail = userEmail;
        SubscriptionLevel = subscriptionLevel;
        IsFreeTier = (subscriptionLevel <= Api.Model.SubscriptionLevel.Free);

        // Use the account's SignUpFrom when it's known; otherwise derive a
        // sensible value from the current subscription level so upgrade
        // prompts always have a tier to target.
        _signUpFrom = signUpFrom != SignUpFrom.None
            ? signUpFrom
            : subscriptionLevel switch
            {
                Api.Model.SubscriptionLevel.Business        => SignUpFrom.Business,
                Api.Model.SubscriptionLevel.Premium         => SignUpFrom.Premium,
                Api.Model.SubscriptionLevel.PasswordManager => SignUpFrom.Premium,
                _                                           => SignUpFrom.Free,
            };

        Notify();
    }

    public void Upgrade()
    {
        IsFreeTier = false;
        Notify();
    }

    public void SetEmail(string email)
    {
        UserEmail = email;
        Notify();
    }

    // ── Helpers ────────────────────────────────────────────────
    public static string BuildDisplayName(string email)
    {
        var local = email.Split('@')[0];
        var parts = local.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(
            " ",
            parts.Select(p => char.ToUpper(p[0]) + (p.Length > 1 ? p[1..] : ""))
        );
    }

    public static string BuildInitials(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "AX";
        }

        var local = email.Split('@')[0];
        var parts = local.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
        return local.Length >= 2
            ? $"{char.ToUpper(local[0])}{char.ToUpper(local[1])}"
            : local[0].ToString().ToUpper();
    }

    public static string BuildAvatarColor(string email)
    {
        var palette = new[]
        {
            "#3b82f6",
            "#8b5cf6",
            "#ec4899",
            "#10b981",
            "#f59e0b",
            "#06b6d4",
            "#ef4444",
            "#14b8a6"
        };
        return palette[Math.Abs(email.GetHashCode()) % palette.Length];
    }
}
