namespace Brandmauer;

public class ThreadsafeContext()
{
    readonly SemaphoreSlim handle = new(1, 1);

    public void Use(Action action)
    {
        handle.Wait();

        try
        {
            action();
        }
        finally
        {
            handle.Release();
        }
    }

    public TResult Use<TResult>(Func<TResult> func)
    {
        handle.Wait();

        TResult result;
        try
        {
            result = func();
        }
        finally
        {
            handle.Release();
        }

        return result;
    }

    public async Task UseAsync(Func<Task> task)
    {
        await handle.WaitAsync();

        try
        {
            await task();
        }
        finally
        {
            handle.Release();
        }
    }

    public async Task<TResult> UseAsync<TResult>(Func<Task<TResult>> task)
    {
        await handle.WaitAsync();

        TResult result;
        try
        {
            result = await task();
        }
        finally
        {
            handle.Release();
        }
        return result;
    }
}
