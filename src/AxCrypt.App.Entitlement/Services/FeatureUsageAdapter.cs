using AxCrypt.Api.Model;
using AxCrypt.App.Entitlement.Contracts;
using AxCrypt.App.Entitlement.Models;
using AxCrypt.Core.Runtime;
using System.Collections.Concurrent;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.App.Entitlement.Services;

/// <summary>
/// Bridges the existing <see cref="UserEntitlementService"/> (which talks to
/// Core's <c>IEntitlementService</c> API client) into the UI-friendly
/// <see cref="IFeatureUsageProvider"/> contract used by FreePlanLimitBar
/// and other Razor consumers.
///
/// Adds two things on top of UserEntitlementService:
///   1. Maps Core's <see cref="LimitedCapability"/> ↔ <see cref="FeatureKey"/>
///      so the UI doesn't need to know about the broader Core enum.
///   2. Wraps the in-memory entitlement map with a thin offline cache
///      (delegates to <see cref="OfflineEntitlementCache"/>) so the
///      FreePlanLimitBar still shows numbers when the API is unreachable,
///      and increments are queued for replay on the next online sync.
/// </summary>
public class FeatureUsageAdapter : IFeatureUsageProvider
{
    private readonly UserEntitlementService _core;
    private readonly OfflineEntitlementCache _cache;

    private readonly ConcurrentDictionary<FeatureKey, FeatureUsage> _snapshot = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string _userEmail = string.Empty;
    private SubscriptionLevel _tier = SubscriptionLevel.Free;

    public FeatureUsageAdapter()
    {
        _core = New<UserEntitlementService>();
        _cache = new OfflineEntitlementCache();
    }

    public event Action? OnChange;

    public bool IsLoading { get; private set; } = true;

    public bool IsOffline { get; private set; }

    // ── Public surface ─────────────────────────────────────────
    public async Task InitializeAsync(string userEmail, SubscriptionLevel currentTier)
    {
        _userEmail = userEmail ?? string.Empty;
        _tier = currentTier;
        IsLoading = true;

        // 1. Seed from the on-disk cache first so the UI has numbers to
        //    render even when we're offline at startup. The snapshot's
        //    IsStale flag tells the UI to caveat freshness.
        IReadOnlyDictionary<FeatureKey, int>? cached = await _cache.LoadAsync(_userEmail);
        if (cached != null)
        {
            foreach (KeyValuePair<FeatureKey, int> kv in cached)
            {
                _snapshot[kv.Key] = BuildUsage(kv.Key, used: kv.Value, isStale: true);
            }
        }
        else
        {
            foreach (FeatureKey f in DefaultFreeLimits.Keys)
            {
                _snapshot[f] = BuildUsage(f, used: 0, isStale: true);
            }
        }

        IsLoading = false;
        Notify();

        // 2. Kick a server refresh in the background. Failures keep us offline.
        _ = RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        if (string.IsNullOrEmpty(_userEmail))
        {
            return;
        }

        try
        {
            await _lock.WaitAsync();
            try
            {
                // Tier is set by the consumer through InitializeAsync /
                // SetTier — we don't reach back into AxCrypt.App.Shared
                // because that would create a circular project reference.
                await _core.InitializeUserUsageLimit(_tier);

                // Replay any offline deltas that were queued while we were down.
                await ReplayPendingAsync();

                // Pull each metered feature's current state into the snapshot.
                Dictionary<FeatureKey, int> fresh = new();
                foreach (FeatureKey f in DefaultFreeLimits.Keys)
                {
                    UsageLimit? u = await _core.GetAvailableUsageLimit(MapToCapability(f), _tier);
                    int used = u?.UsedCount ?? 0;
                    int max = u?.MaxCount ?? DefaultFreeLimits[f];
                    fresh[f] = used;
                    _snapshot[f] = new FeatureUsage
                    {
                        Feature = f,
                        Used = used,
                        Limit = LimitFor(f, max),
                        LastSyncedUtc = DateTime.UtcNow,
                        IsStale = false,
                    };
                }

                await _cache.SaveAsync(_userEmail, fresh);
            }
            finally
            {
                _lock.Release();
            }

            IsOffline = false;
            Notify();
        }
        catch
        {
            // API unreachable — fall back to whatever the cache + memory
            // already hold. The UI just shows "stale" badges.
            IsOffline = true;
            Notify();
        }
    }

    public FeatureUsage GetUsage(FeatureKey feature)
    {
        if (_snapshot.TryGetValue(feature, out FeatureUsage? u) && u != null)
        {
            return u;
        }
        return BuildUsage(feature, used: 0, isStale: true);
    }

    public async Task<bool> TryConsumeAsync(FeatureKey feature, int count = 1)
    {
        if (count <= 0) return true;

        // Premium / Business have no caps and don't write counters.
        if (_tier > SubscriptionLevel.Lite) return true;

        await _lock.WaitAsync();
        try
        {
            FeatureUsage current = GetUsage(feature);

            // Pre-flight gate — reject if the action would exceed the cap.
            if (current.IsExhausted || current.Remaining < count)
            {
                return false;
            }

            await PersistIncrementAsync(feature, count, current);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RecordUsageAsync(FeatureKey feature, int count = 1)
    {
        if (count <= 0) return;

        // Premium / Business have no caps and don't write counters.
        if (_tier > SubscriptionLevel.Lite) return;

        await _lock.WaitAsync();
        try
        {
            // No cap check here — unlike TryConsumeAsync this records an
            // action that has ALREADY happened (e.g. files just encrypted),
            // so the counter must reflect reality even if it tips the user
            // over the free cap.
            await PersistIncrementAsync(feature, count, GetUsage(feature));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Apply an increment to the snapshot, the offline cache and the server.
    /// The caller MUST hold <see cref="_lock"/>. The snapshot bump + first
    /// <see cref="Notify"/> happen before the (slower) server round-trip so
    /// the FreePlanLimitBar moves the instant the usage is recorded.
    /// </summary>
    private async Task PersistIncrementAsync(FeatureKey feature, int count, FeatureUsage current)
    {
        int newUsed = current.Used + count;
        _snapshot[feature] = new FeatureUsage
        {
            Feature = feature,
            Used = newUsed,
            Limit = current.Limit,
            LastSyncedUtc = DateTime.UtcNow,
            IsStale = current.IsStale,
        };

        // Surface the new number immediately — before any I/O.
        Notify();

        // Persist locally even if the server call fails — we don't want the
        // local counter to drift.
        await _cache.SaveUsageAsync(_userEmail, feature, newUsed);

        try
        {
            // One call per unit so the server's increment stays consistent
            // regardless of batch size.
            for (int i = 0; i < count; i++)
            {
                await _core.InsertUserUsageCount(MapToCapability(feature), _tier);
            }
            IsOffline = false;
        }
        catch
        {
            // Server unreachable — queue the delta for replay on next sync.
            await _cache.QueuePendingAsync(_userEmail, feature, count);
            IsOffline = true;
        }

        // Refresh again so the offline / stale badge reflects the outcome.
        Notify();
    }

    public void Clear()
    {
        _snapshot.Clear();
        _userEmail = string.Empty;
        _tier = SubscriptionLevel.Free;
        IsOffline = false;
        IsLoading = true;
        Notify();
    }

    public void SetTier(SubscriptionLevel tier)
    {
        if (_tier == tier) return;
        _tier = tier;
        // Re-emit usage with the new limit so the UI flips from
        // "X / 3" to "unlimited" (or vice-versa) immediately.
        foreach (FeatureKey f in DefaultFreeLimits.Keys)
        {
            FeatureUsage current = GetUsage(f);
            _snapshot[f] = new FeatureUsage
            {
                Feature = f,
                Used = current.Used,
                Limit = LimitFor(f, DefaultFreeLimits[f]),
                LastSyncedUtc = current.LastSyncedUtc,
                IsStale = current.IsStale,
            };
        }
        Notify();
    }

    // ── Internals ──────────────────────────────────────────────
    private async Task ReplayPendingAsync()
    {
        IReadOnlyList<(FeatureKey feature, int count)> pending = await _cache.GetPendingAsync(_userEmail);
        if (pending.Count == 0) return;

        foreach ((FeatureKey f, int c) in pending)
        {
            try
            {
                await _core.SyncUserUsageCountAsync(MapToCapability(f), _tier, c);
            }
            catch
            {
                // Still offline — leave the queue intact for next time.
                return;
            }
        }
        await _cache.ClearPendingAsync(_userEmail);
    }

    private static LimitedCapability MapToCapability(FeatureKey f) => f switch
    {
        FeatureKey.FileEncryption => LimitedCapability.StrongerEncryption,
        FeatureKey.KeyShare => LimitedCapability.KeySharing,
        FeatureKey.PasswordEntry => LimitedCapability.CreateSecret,
        FeatureKey.SecuredMessage => LimitedCapability.SendSecuredMessages,
        _ => LimitedCapability.StrongerEncryption,
    };

    private FeatureUsage BuildUsage(FeatureKey f, int used, bool isStale)
    {
        int defaultCap = DefaultFreeLimits.TryGetValue(f, out int c) ? c : 0;
        return new FeatureUsage
        {
            Feature = f,
            Used = used,
            Limit = LimitFor(f, defaultCap),
            LastSyncedUtc = DateTime.UtcNow,
            IsStale = isStale,
        };
    }

    private int LimitFor(FeatureKey f, int defaultCap)
    {
        if (_tier > SubscriptionLevel.Lite) return -1; // Premium / Business → unlimited
        return defaultCap;
    }

    private static readonly Dictionary<FeatureKey, int> DefaultFreeLimits = new()
    {
        { FeatureKey.FileEncryption, 2 },
        { FeatureKey.KeyShare,       1 },
        { FeatureKey.PasswordEntry,  10 },
        { FeatureKey.SecuredMessage, 10 },
    };

    private void Notify() => OnChange?.Invoke();
}

// No back-reference to AxCrypt.App.Shared from this project — the
// tier is supplied via InitializeAsync / SetTier so the dependency
// direction stays one-way (Shared → Entitlement).