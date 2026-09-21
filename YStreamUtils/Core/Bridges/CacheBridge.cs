using Jint;
using Jint.Native;
using Jint.Native.Json;
using YStreamUtils.Core.Data;
using YStreamUtils.Core.Entities;

namespace YStreamUtils.Core.Bridges;

public class CacheBridge(string name, AppDbContext dbContext) : IBridge
{
    private readonly ReaderWriterLockSlim _lock = new();

    private string CreateKey(string key) => $"{name}:{key}";
    
    public void Register(Engine vm, JsObject hostObj)
    {
        var cacheObj = new JsObject(vm);

        cacheObj.Set("get", JsValue.FromObject(vm, new Func<string, JsValue>((key) =>
        {
            _lock.EnterReadLock();
            try
            {
                var fullKey = CreateKey(key);
                var cacheEntry = dbContext.Caches.FirstOrDefault(x => x.Key == fullKey);
                
                if (cacheEntry == null || string.IsNullOrEmpty(cacheEntry.Value))
                {
                    return JsValue.Null;
                }

                var parser = new JsonParser(vm);
                return parser.Parse(cacheEntry.Value);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        })));

        cacheObj.Set("set", JsValue.FromObject(vm, new Action<string, JsValue>((key, val) =>
        {
            _lock.EnterWriteLock();
            try
            {
                var fullKey = CreateKey(key);
                
                var serializer = new JsonSerializer(vm);
                JsValue stringified = serializer.Serialize(val, JsValue.Undefined, JsValue.Undefined);
                string stringValue = stringified.AsString();

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
        })));

        cacheObj.Set("delete", JsValue.FromObject(vm, new Action<string>((key) =>
        {
            _lock.EnterWriteLock();
            try
            {
                var fullKey = CreateKey(key);
                var cacheEntry = dbContext.Caches.FirstOrDefault(x => x.Key == fullKey);
                
                if (cacheEntry != null)
                {
                    dbContext.Caches.Remove(cacheEntry);
                    dbContext.SaveChanges();
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        })));

        cacheObj.Set("clear", JsValue.FromObject(vm, new Action(() =>
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
        })));

        hostObj.Set("cache", cacheObj);
    }
}
