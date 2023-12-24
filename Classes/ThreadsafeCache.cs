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

    protected abstract TValue GetNew(Dictionary<TKey, TempValue> x, TKey key);

    public TValue Get(TKey key)
    {
        return cache.Use(x => GetUnsafe(x, key));
    }

    public TValue GetUnsafe(Dictionary<TKey, TempValue> x, TKey key)
    {
        if (x.TryGetValue(key, out var tempValue))
        {
            if (!MaxAge.HasValue)
                return tempValue.value;

            if ((DateTime.Now - tempValue.timestamp) < MaxAge)
                return tempValue.value;

            x.Remove(key);

            if (Logging)
                Audit.Info(GetType(), $"{key.ToJson()} removed.");
        }

        tempValue = new()
        {
            timestamp = DateTime.Now,
            value = GetNew(x, key)
        };

        if (Logging)
            Audit.Info(GetType(), $"Try adding {key.ToJson()}.");

        if (tempValue.value is null)
        {
            x.Add(key, tempValue);
            Audit.Info(GetType(), $"{tempValue.ToJson()} added.");
        }
        else
        {
            Audit.Warning(
                GetType(),
                $"Could not add {key.ToJson()} because value is null."
            );
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
