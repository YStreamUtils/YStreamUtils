using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;

namespace YStreamUtils.Core.Bridges;

public class CacheBridge(string name, AppDbContext dbContext)
{
    private readonly ReaderWriterLockSlim _lock = new();

    private string CreateKey(string key) => $"{name}:{key}";

    public object? Get(string key)
    {
        _lock.EnterReadLock();
        try
        {
            var fullKey = CreateKey(key);
            var cacheEntry = dbContext.Caches.FirstOrDefault(x => x.Key == fullKey);

            if (cacheEntry == null || string.IsNullOrEmpty(cacheEntry.Value))
            {
                return null;
            }

            return cacheEntry.Value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Set(string key, object val)
    {
        _lock.EnterWriteLock();
        try
        {
            var fullKey = CreateKey(key);
            var stringValue = val.ToString() ?? string.Empty;

            var cacheEntry = dbContext.Caches.FirstOrDefault(x => x.Key == fullKey);
            if (cacheEntry != null)
            {
                cacheEntry.Value = stringValue;
            }
            else
            {
                dbContext.Caches.Add(new Cache
                {
                    Key = fullKey,
                    Value = stringValue
                });
            }

            dbContext.SaveChanges();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Delete(string key)
    {
        _lock.EnterWriteLock();
        try
        {
            var fullKey = CreateKey(key);
            var cacheEntry = dbContext.Caches.FirstOrDefault(x => x.Key == fullKey);

            if (cacheEntry == null) return;

            dbContext.Caches.Remove(cacheEntry);
            dbContext.SaveChanges();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            var prefix = $"{name}:";
            var entriesToRemove = dbContext.Caches.Where(x => x.Key.StartsWith(prefix));

            dbContext.Caches.RemoveRange(entriesToRemove);
            dbContext.SaveChanges();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
}