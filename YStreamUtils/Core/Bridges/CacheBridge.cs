using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace YStreamUtils.Core.Bridges;

public class CacheBridge(string name) : IBridge
{
    private readonly string _name = name;
    private readonly ReaderWriterLockSlim _lock = new();
    
    private readonly Dictionary<string, JsValue> _data = new();

    public void Register(Engine vm, JsObject hostObj)
    {
        var cacheObj = new JsObject(vm);

        cacheObj.Set("get", JsValue.FromObject(vm, new Func<string, JsValue>((key) =>
        {
            _lock.EnterReadLock();
            try
            {
                return _data.TryGetValue(key, out var val) ? val : JsValue.Null;
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
                _data[key] = val;
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
                _data.Remove(key);
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
                _data.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        })));

        hostObj.Set("cache", cacheObj);
    }
}
