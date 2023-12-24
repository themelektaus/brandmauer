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
        var t = GetType();

        if (x.TryGetValue(key, out var tempValue))
        {
            if (!MaxAge.HasValue)
                return tempValue.value;

            if ((DateTime.Now - tempValue.timestamp) < MaxAge)
                return tempValue.value;

            x.Remove(key);

            if (Logging)
                Audit.Info(t, $"{key.ToJson()} removed.");
        }

        tempValue = new()
        {
            timestamp = DateTime.Now,
            value = GetNew(x, key)
        };

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
            x.Add(key, tempValue);
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
