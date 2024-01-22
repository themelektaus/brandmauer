namespace Brandmauer;

public abstract class AsyncThreadsafeCache<TKey, TValue>
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

    protected abstract Task<TValue> GetNewAsync(TKey key);

    public async Task<TValue> GetAsync(TKey key)
    {
        return await cache.UseAsync(x => GetUnsafeAsync(key, x));
    }

    public async Task<TValue> GetUnsafeAsync(TKey key)
    {
        return await GetUnsafeAsync(key, CurrentCache);
    }

    async Task<TValue> GetUnsafeAsync(
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
                value = await GetNewAsync(key)
            };
        }
        cacheStack.Pop();

        var k = key.ToJson();

        if (Logging)
            Audit.Info(t, $"Try adding {k}.");

        if (tempValue.value is null)
        {
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
