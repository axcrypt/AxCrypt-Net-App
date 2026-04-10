using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AxCrypt.App.Entitlement.Contracts;

namespace AxCrypt.App.Entitlement.Services;

/// <summary>
/// JSON-file-backed offline cache for entitlement counters. Stores
/// <list type="bullet">
///   <item>a snapshot of <see cref="FeatureKey"/> → used count</item>
///   <item>a queue of pending offline increments for replay</item>
/// </list>
/// per-user, in <c>%LocalAppData%/AxCrypt/Entitlement/</c>. Filenames are
/// hashed so the email never appears on disk. Writes are temp-then-rename
/// so a crash mid-write can't corrupt the cache.
///
/// Used by <see cref="FeatureUsageAdapter"/>; not exposed as a DI service.
/// </summary>
public class OfflineEntitlementCache
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        IncludeFields = false,
    };

    private readonly string _baseDir;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public OfflineEntitlementCache(string? baseDir = null)
    {
        _baseDir = baseDir ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AxCrypt",
            "Entitlement");
        try { Directory.CreateDirectory(_baseDir); }
        catch { /* ignored — we'll surface IO errors at write time */ }
    }

    public async Task<IReadOnlyDictionary<FeatureKey, int>?> LoadAsync(string userEmail)
    {
        await _lock.WaitAsync();
        try
        {
            string path = SnapshotPath(userEmail);
            if (!File.Exists(path)) return null;
            await using FileStream s = File.OpenRead(path);
            Dictionary<FeatureKey, int>? d = await JsonSerializer
                .DeserializeAsync<Dictionary<FeatureKey, int>>(s, JsonOpts);
            return d;
        }
        catch
        {
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(string userEmail, IReadOnlyDictionary<FeatureKey, int> usage)
    {
        await _lock.WaitAsync();
        try
        {
            await WriteAtomicAsync(SnapshotPath(userEmail), usage.ToDictionary(kv => kv.Key, kv => kv.Value));
        }
        catch { /* IO failure → next save attempt overwrites */ }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveUsageAsync(string userEmail, FeatureKey feature, int newUsed)
    {
        await _lock.WaitAsync();
        try
        {
            string path = SnapshotPath(userEmail);
            Dictionary<FeatureKey, int> map;
            if (File.Exists(path))
            {
                await using FileStream s = File.OpenRead(path);
                map = (await JsonSerializer.DeserializeAsync<Dictionary<FeatureKey, int>>(s, JsonOpts))
                      ?? new Dictionary<FeatureKey, int>();
            }
            else
            {
                map = new Dictionary<FeatureKey, int>();
            }

            map[feature] = newUsed;
            await WriteAtomicAsync(path, map);
        }
        catch { /* swallow IO errors */ }
        finally
        {
            _lock.Release();
        }
    }

    public async Task QueuePendingAsync(string userEmail, FeatureKey feature, int count)
    {
        await _lock.WaitAsync();
        try
        {
            List<PendingDelta> queue = await LoadPendingNoLockAsync(userEmail);
            queue.Add(new PendingDelta { Feature = feature, Count = count, QueuedUtc = DateTime.UtcNow });
            await WriteAtomicAsync(PendingPath(userEmail), queue);
        }
        catch { }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<(FeatureKey feature, int count)>> GetPendingAsync(string userEmail)
    {
        await _lock.WaitAsync();
        try
        {
            List<PendingDelta> queue = await LoadPendingNoLockAsync(userEmail);
            return queue.Select(d => (d.Feature, d.Count)).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearPendingAsync(string userEmail)
    {
        await _lock.WaitAsync();
        try
        {
            string path = PendingPath(userEmail);
            if (File.Exists(path)) File.Delete(path);
            await Task.CompletedTask;
        }
        catch { }
        finally
        {
            _lock.Release();
        }
    }

    // ── Helpers ────────────────────────────────────────────────
    private string SnapshotPath(string email) =>
        Path.Combine(_baseDir, $"{HashEmail(email)}.snapshot.json");

    private string PendingPath(string email) =>
        Path.Combine(_baseDir, $"{HashEmail(email)}.pending.json");

    private static string HashEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "anonymous";
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(email.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
    }

    private async Task<List<PendingDelta>> LoadPendingNoLockAsync(string userEmail)
    {
        string path = PendingPath(userEmail);
        if (!File.Exists(path)) return new List<PendingDelta>();
        try
        {
            await using FileStream s = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<PendingDelta>>(s, JsonOpts)
                   ?? new List<PendingDelta>();
        }
        catch
        {
            return new List<PendingDelta>();
        }
    }

    private static async Task WriteAtomicAsync<T>(string path, T value)
    {
        string tmp = path + ".tmp";
        await using (FileStream s = File.Create(tmp))
        {
            await JsonSerializer.SerializeAsync(s, value, JsonOpts);
        }
        if (File.Exists(path)) File.Replace(tmp, path, destinationBackupFileName: null);
        else File.Move(tmp, path);
    }

    private sealed class PendingDelta
    {
        public FeatureKey Feature { get; set; }
        public int Count { get; set; }
        public DateTime QueuedUtc { get; set; } = DateTime.UtcNow;
    }
}
