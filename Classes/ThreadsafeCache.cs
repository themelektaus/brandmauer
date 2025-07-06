namespace Brandmauer;

public abstract class ThreadsafeCache<TKey, TValue>
{
    public struct TempValue
    {
        public DateTime timestamp;
        public TValue value;
    }

    readonly ThreadsafeObject<Dictionary<TKey, TempValue>> cache = new(new());

    protected abstract bool Logging { get; }

    protected abstract TimeSpan? MaxAge { get; }

    readonly Stack<Dictionary<TKey, TempValue>> cacheStack = new();

    protected Dictionary<TKey, TempValue> CurrentCache => cacheStack.Peek();

    protected abstract TValue GetNew(TKey key);

    public TValue Get(TKey key)
    {
        return cache.Use(x => GetUnsafe(key, x));
    }

    public TValue GetUnsafe(TKey key)
    {
        return GetUnsafe(key, CurrentCache);
    }

    TValue GetUnsafe(
        TKey key,
        Dictionary<TKey, TempValue> currentCache
    )
    {
        var t = GetType();

        if (currentCache.TryGetValue(key, out var tempValue))
        {
            if (!MaxAge.HasValue)
                return tempValue.value;

            if ((DateTime.Now - tempValue.timestamp) < MaxAge)
                return tempValue.value;

            currentCache.Remove(key);

            if (Logging)
                Audit.Info(t, $"{key.ToJson()} removed.");
        }

        cacheStack.Push(currentCache);
        {
            tempValue = new()
            {
                timestamp = DateTime.Now,
                value = GetNew(key)
            };
        }
        cacheStack.Pop();

        var k = key.ToJson();

        if (Logging)
            Audit.Info(t, $"Try adding {k}.");

        if (tempValue.value is null)
        {
            if (Logging)
                Audit.Warning(
                    GetType(),
                    $"Could not add {k} because value is null."
                );
        }
        else
        {
            currentCache.Add(key, tempValue);
            var v = tempValue.value.ToJson();
            if (v.Length > 90)
                v = $"{v[..90]}...";

            if (Logging)
                Audit.Info(t, $"Added {k} with value {v}.");
        }

        return tempValue.value;
    }

    public void Clear()
    {
        cache.Use(x => x.Clear());
    }

    public void Clear(TKey key)
    {
        cache.Use(x => x.Remove(key));
    }
}
