using System;
using System.Threading.Tasks;
using AxCrypt.Api.Model;

namespace AxCrypt.App.Entitlement.Contracts;

/// <summary>
/// UI-binding contract for the entitlement / quota subsystem.
///
/// Sits one layer above <c>AxCrypt.App.Entitlement.Services.UserEntitlementService</c>
/// (which talks to <c>AxCrypt.Core.Service.Entitlement.IEntitlementService</c>)
/// and exposes a small, Razor-friendly read model: one <see cref="FeatureUsage"/>
/// per <see cref="FeatureKey"/>.
///
/// Online behavior:  <see cref="RefreshAsync"/> pulls from the API and primes the
///                   in-memory + on-disk cache.
/// Offline behavior: <see cref="GetUsage"/> serves the on-disk cache with
///                   <c>IsStale = true</c>; <see cref="TryConsumeAsync"/> queues
///                   the increment for replay on next online cycle.
///
/// Premium / Business users always return <c>Limit = -1</c> (unlimited).
/// </summary>
public interface IFeatureUsageProvider
{
    /// <summary>Raised whenever any feature counter changes.</summary>
    event Action? OnChange;

    /// <summary>True until the first snapshot has loaded.</summary>
    bool IsLoading { get; }

    /// <summary>True when operating from cache (no recent successful API sync).</summary>
    bool IsOffline { get; }

    /// <summary>
    /// Prime the provider for the signed-in user.
    /// <paramref name="currentTier"/> is the live subscription level —
    /// the provider uses it to decide which API endpoint to hit (Free
    /// → non-paying, Lite → lite, anything above → no API needed).
    /// Passed in rather than looked up so this project doesn't have to
    /// reach back into AxCrypt.App.Shared (the dependency direction
    /// goes Shared → Entitlement only).
    /// </summary>
    Task InitializeAsync(string userEmail, SubscriptionLevel currentTier);

    /// <summary>Force a refresh from the API. Silent no-op when offline.</summary>
    Task RefreshAsync();

    /// <summary>Update the tier (e.g. on plan change) without re-initializing.</summary>
    void SetTier(SubscriptionLevel tier);

    /// <summary>Latest usage + cap for a feature. Always returns a valid value.</summary>
    FeatureUsage GetUsage(FeatureKey feature);

    /// <summary>Latest usage + cap for a feature. Always returns a valid value.</summary>
    void UpdateUsageCount(FeatureKey feature, int increment = 1);

    /// <summary>Convenience check — true when the user has remaining allowance.</summary>
    bool CanUse(FeatureKey feature) => !GetUsage(feature).IsExhausted;

    /// <summary>
    /// Record a usage increment. Returns false when the increment would
    /// exceed the cap. Offline increments are queued for replay.
    /// Use this as a pre-flight gate <em>before</em> performing the action.
    /// </summary>
    Task<bool> TryConsumeAsync(FeatureKey feature, int count = 1);

    /// <summary>
    /// Record usage that has <em>already</em> happened (e.g. files just
    /// encrypted). Unlike <see cref="TryConsumeAsync"/> this never rejects —
    /// the action is done, so the counter must reflect it even if it tips
    /// the user over the free cap. Bumps the snapshot immediately (so the
    /// FreePlanLimitBar moves at once), persists to the offline cache, and
    /// reports the increment to the server (queued for replay when
    /// offline). No-op for Premium / Business (unmetered).
    /// </summary>
    Task RecordUsageAsync(FeatureKey feature, int count = 1);

    /// <summary>Drop the snapshot on sign-out.</summary>
    void Clear();
}
