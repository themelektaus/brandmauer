using System.Reflection;

namespace Brandmauer;

public abstract class IntervalTask : IAsyncDisposable
{
    protected abstract Task OnStartAsync();
    protected abstract Task OnBeforeFirstTickAsync();
    protected abstract Task OnTickAsync();
    protected abstract Task OnDisposeAsync();

    readonly TimeSpan delay;
    readonly TimeSpan interval;

    Task task;
    CancellationTokenSource ctSource;

    protected bool disposed;

    public IntervalTask()
    {
        var t = GetType();

        delay = TimeSpan.FromSeconds(
            t.GetCustomAttribute<DelayAttribute>()?.seconds ?? 0
        );

        interval = TimeSpan.FromSeconds(
            t.GetCustomAttribute<IntervalAttribute>()?.seconds ?? 1
        );

    }

    public void RunInBackground()
    {
        ctSource = new();
        var ct = ctSource.Token;

        task = Task.Run(async () =>
        {
            await (OnStartAsync() ?? Task.CompletedTask);

            try { await Task.Delay(delay, ct); } catch { }

            await (OnBeforeFirstTickAsync() ?? Task.CompletedTask);

            while (!disposed)
            {
                await (OnTickAsync() ?? Task.CompletedTask);
                try { await Task.Delay(interval, ct); } catch { }
            }
        }, ct);
    }

    public async ValueTask DisposeAsync()
    {
        var x = GetType().Name;

        if (disposed)
        {
            Utils.Log("Dispose", $"{x} already disposed. Skipping...");
            return;
        }

        Utils.Log("Dispose", $"Waiting for {x} ...");
        disposed = true;

        await (OnDisposeAsync() ?? Task.CompletedTask);

        Utils.Log("Dispose", $"Send Cancellation Request for {x}");
        ctSource?.Cancel(false);

        if (task is not null)
        {
            Utils.Log("Dispose", $"Waiting for internal task of {x}");
            await task;
            task = null;
        }
    }
}
