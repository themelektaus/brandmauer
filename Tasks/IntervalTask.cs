namespace Brandmauer;

public abstract partial class IntervalTask : IAsyncDisposable
{
    protected abstract TimeSpan Delay { get; }
    protected abstract TimeSpan Interval { get; }

    protected abstract Task OnStartAsync();
    protected abstract Task OnBeforeFirstTickAsync();
    protected abstract Task OnTickAsync();
    protected abstract Task OnDisposeAsync();

    Task task;
    CancellationTokenSource ctSource;

    protected bool disposed;

    public void RunInBackground()
    {
        ctSource = new();
        var ct = ctSource.Token;

        task = Task.Run(async () =>
        {
            await OnStartAsync();

            try { await Task.Delay(Delay, ct); } catch { }

            await OnBeforeFirstTickAsync();

            while (!disposed)
            {
                await OnTickAsync();
                try { await Task.Delay(Interval, ct); } catch { }
            }
        }, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;

        await OnDisposeAsync();

        ctSource?.Cancel(false);

        if (task is not null)
        {
            await task;
            task = null;
        }
    }
}
