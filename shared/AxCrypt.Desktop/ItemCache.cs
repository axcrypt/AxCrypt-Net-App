using AxCrypt.Abstractions;
using AxCrypt.Common;
using static AxCrypt.Abstractions.TypeResolve;

namespace AxCrypt.Desktop;

public class ItemCache : ICache, IDisposable
{
    private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    private INow _now;

    private Dictionary<string, CacheStoredValue> _cache = new Dictionary<string, CacheStoredValue>();

    private static readonly object _object = new object();

    public ItemCache()
        : this(New<INow>())
    {
    }

    public ItemCache(INow dateTimeProvider)
    {
        _now = dateTimeProvider;
    }

    public T GetItem<T>(ICacheKey cacheKey, Func<T> itemFunction)
    {
        if (cacheKey == null)
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }
        if (itemFunction == null)
        {
            throw new ArgumentNullException(nameof(itemFunction));
        }

        _lock.Wait();
        try
        {
            object o = GetFromCache(cacheKey.Key);
            if (o != null)
            {
                return (T)o;
            }
            T item = itemFunction();
            PutIntoCache(cacheKey, item, _now.Utc);
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> GetItemAsync<T>(ICacheKey cacheKey, Func<Task<T>> itemFunctionAsync)
    {
        await _lock.WaitAsync().Free();
        try
        {
            object o = GetFromCache(cacheKey.Key);
            if (o != null)
            {
                return (T)o;
            }
            T item = await itemFunctionAsync().Free();
            if (item != null)
            {
                PutIntoCache(cacheKey, item, _now.Utc);
            }
            return item;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void UpdateItem(Action updateAction, params ICacheKey[] dependencies)
    {
        if (updateAction == null)
        {
            throw new ArgumentNullException(nameof(updateAction));
        }
        if (dependencies == null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        _lock.Wait();
        try
        {
            updateAction();
        }
        finally
        {
            foreach (ICacheKey key in dependencies)
            {
                RemoveKey(key.Key);
            }

            _lock.Release();
        }
    }

    public async Task UpdateItemAsync(Func<Task> updateFunctionAsync, params ICacheKey[] dependencies)
    {
        await _lock.WaitAsync().Free();
        try
        {
            await updateFunctionAsync().Free();
        }
        finally
        {
            foreach (ICacheKey key in dependencies)
            {
                RemoveKey(key.Key);
            }

            _lock.Release();
        }
    }

    public async Task<T> UpdateItemAsync<T>(Func<Task<T>> updateFunctionAsync, params ICacheKey[] dependencies)
    {
        await _lock.WaitAsync().Free();
        try
        {
            T item = await updateFunctionAsync().Free();
            return item;
        }
        finally
        {
            foreach (ICacheKey key in dependencies)
            {
                RemoveKey(key.Key);
            }
            _lock.Release();
        }
    }

    public void RemoveItem(ICacheKey cacheKey)
    {
        if (cacheKey == null)
        {
            throw new ArgumentNullException(nameof(cacheKey));
        }

        RemoveKey(cacheKey.Key);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        if (_lock != null)
        {
            _lock.Dispose();
            _lock = null;
        }

        if (_cache != null)
        {
            _cache.Clear();
            _cache = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private object GetFromCache(string key)
    {
        if (IsExpired(key, _now.Utc))
        {
            return null;
        }

        CacheStoredValue storedValue = null;
        if (!_cache.TryGetValue(key, out storedValue))
        {
            return null;
        }

        return storedValue.Value;
    }

    private bool IsExpired(string key, DateTime currentTime)
    {
        CacheStoredValue storedValue = null;
        if (!_cache.TryGetValue(key, out storedValue))
        {
            return true;
        }

        bool isExpired = false;
        if (storedValue.DependentParent != null)
        {
            isExpired = IsExpired(storedValue.DependentParent, currentTime);
        }

        if (!isExpired)
        {
            isExpired = storedValue.AbsoluteExpiration < currentTime;
        }

        if (isExpired)
        {
            RemoveKey(key);
        }

        return isExpired;
    }

    private void RemoveKey(string key)
    {
        CacheStoredValue storedValue = null;
        bool isExist = _cache.TryGetValue(key, out storedValue);
        if (!isExist)
        {
            return;
        }

        if (storedValue.DependentParent != null)
        {
            RemoveLinkInParent(key, storedValue.DependentParent);
        }

        RemoveItemAndChildsTree(key);
    }

    private void RemoveLinkInParent(string itemKey, string parentKey)
    {
        CacheStoredValue parentValue = null;
        bool isExist = _cache.TryGetValue(parentKey, out parentValue);
        if (isExist)
        {
            parentValue.DependentChildren.Remove(itemKey);
        }
    }

    private void RemoveItemAndChildsTree(string key)
    {
        CacheStoredValue storedValue = null;
        bool isExist = _cache.TryGetValue(key, out storedValue);
        if (!isExist)
        {
            return;
        }

        _cache.Remove(key);
        foreach (string childKey in storedValue.DependentChildren)
        {
            RemoveItemAndChildsTree(childKey);
        }
    }

    private void PutIntoCache(ICacheKey key, object value, DateTime currentTime)
    {
        RemoveExpiredValues(key);
        CacheStoredValue storedValue = null;
        bool isExist = _cache.TryGetValue(key.Key, out storedValue);
        if (isExist)
        {
            // Remove link to old parent
            if (storedValue.DependentParent != null)
            {
                RemoveLinkInParent(key.Key, storedValue.DependentParent);
            }
        }
        else
        {
            // Create new value
            storedValue = new CacheStoredValue
            {
                Value = value,
            };
        }

        AddParentKeyIfRequired(key, currentTime);

        UpdateStoredValueFromNewKey(key, storedValue, currentTime);

        _cache[key.Key] = storedValue;
    }

    private void RemoveExpiredValues(ICacheKey key)
    {
        IsExpired(key.Key, _now.Utc);
    }

    private static void UpdateStoredValueFromNewKey(ICacheKey key, CacheStoredValue storedValue, DateTime currentTime)
    {
        storedValue.DependentParent = null;
        if (key.ParentCacheKey != null)
        {
            storedValue.DependentParent = key.ParentCacheKey.Key;
        }

        storedValue.AbsoluteExpiration = DateTime.MaxValue;
        if (key.Expiration != TimeSpan.Zero)
        {
            storedValue.AbsoluteExpiration = currentTime + key.Expiration;
        }
    }

    private void AddParentKeyIfRequired(ICacheKey key, DateTime currentTime)
    {
        if (key.ParentCacheKey == null)
        {
            return;
        }

        AddParentKeyIfRequired(key.ParentCacheKey, currentTime);

        CacheStoredValue parentValue = null;
        bool isExist = _cache.TryGetValue(key.ParentCacheKey.Key, out parentValue);
        if (isExist)
        {
            if (!parentValue.DependentChildren.Contains(key.Key))
            {
                parentValue.DependentChildren.Add(key.Key);
            }

            return;
        }

        CacheStoredValue newParentValue = new CacheStoredValue
        {
            Value = _object,
        };

        UpdateStoredValueFromNewKey(key.ParentCacheKey, newParentValue, currentTime);
        if (!newParentValue.DependentChildren.Contains(key.Key))
        {
            newParentValue.DependentChildren.Add(key.Key);
        }

        _cache[key.ParentCacheKey.Key] = newParentValue;
    }
}