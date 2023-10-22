namespace Brandmauer;

public class ThreadsafeObject<T>(T value)
{
    readonly SemaphoreSlim handle = new(1, 1);

    public void Use(Action<T> action)
    {
        handle.Wait();

        try
        {
            action(value);
        }
        finally
        {
            handle.Release();
        }
    }

    public TResult Use<TResult>(Func<T, TResult> func)
    {
        handle.Wait();

        TResult result;
        try
        {
            result = func(value);
        }
        finally
        {
            handle.Release();
        }

        return result;
    }

    public async Task UseAsync(Func<T, Task> task)
    {
        await handle.WaitAsync();

        try
        {
            await task(value);
        }
        finally
        {
            handle.Release();
        }
    }

    public async Task<TResult> UseAsync<TResult>(Func<T, Task<TResult>> task)
    {
        await handle.WaitAsync();

        TResult result;
        try
        {
            result = await task(value);
        }
        finally
        {
            handle.Release();
        }
        return result;
    }
}
