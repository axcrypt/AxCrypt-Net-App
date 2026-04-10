using System;

namespace AxCrypt.App.Entitlement.Contracts;

/// <summary>
/// Current usage + cap for a single metered feature. Returned by
/// <see cref="IFeatureUsageProvider.GetUsage"/> and rendered by the
/// shared FreePlanLimitBar component.
/// </summary>
public sealed class FeatureUsage
{
    public FeatureKey Feature { get; init; }

    /// <summary>Times the user has consumed this feature in the current period.</summary>
    public int Used { get; init; }

    /// <summary>Plan cap. -1 means unlimited (Premium / Business).</summary>
    public int Limit { get; init; }

    /// <summary>True when this user has unlimited use of the feature.</summary>
    public bool IsUnlimited => Limit < 0;

    /// <summary>Remaining allowance. 0 when at cap; <c>int.MaxValue</c> when unlimited.</summary>
    public int Remaining =>
        IsUnlimited ? int.MaxValue : Math.Max(0, Limit - Used);

    /// <summary>True when the user can't perform this action any more on the current plan.</summary>
    public bool IsExhausted => !IsUnlimited && Used >= Limit;

    /// <summary>Used as a percentage of the cap, clamped to 0–100. Always 0 when unlimited.</summary>
    public int PercentUsed
    {
        get
        {
            if (IsUnlimited || Limit <= 0) return 0;
            int pct = Used * 100 / Limit;
            return pct < 0 ? 0 : (pct > 100 ? 100 : pct);
        }
    }

    /// <summary>UTC instant when the snapshot was last read (from server or local cache).</summary>
    public DateTime LastSyncedUtc { get; init; } = DateTime.UtcNow;

    /// <summary>True when the snapshot came from the local offline cache rather than a fresh API call.</summary>
    public bool IsStale { get; init; }
}
