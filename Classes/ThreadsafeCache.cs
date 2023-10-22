﻿namespace Brandmauer;

public abstract class ThreadsafeCache<TKey, TValue>
{
    readonly ThreadsafeObject<Dictionary<TKey, TValue>> cache = new(new());

    protected abstract bool Logging { get; }
    protected abstract TValue GetNew(TKey key);

    public TValue Get(TKey key)
    {
        return cache.Use(x =>
        {
            if (x.TryGetValue(key, out var value))
                return value;

            value = GetNew(key);

            if (Logging)
            {
                var name = GetType().FullName;
                Console.WriteLine($"{name}.GetNew({key.ToJson()}) => {value.ToJson()}");
            }

            if (value is not null)
                x.Add(key, value);

            return value;
        });
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
