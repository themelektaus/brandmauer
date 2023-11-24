namespace Brandmauer;

public abstract partial class IntervalTask : IAsyncDisposable
{
    protected abstract TimeSpan Delay { get; }
    protected abstract TimeSpan Interval { get; }

    protected abstract Task OnTickAsync();

    Task task;
    CancellationTokenSource ctSource;

    bool disposed;

    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;

        ctSource?.Cancel(false);
        
        if (task is not null)
        {
            await task;
            task = null;
        }
    }

    public void RunInBackground()
    {
        ctSource = new();
        var ct = ctSource.Token;

        task = Task.Run(async () =>
        {
            try { await Task.Delay(Delay, ct); } catch { }

            while (!disposed)
            {
                await OnTickAsync();
                try { await Task.Delay(Interval, ct); } catch { }
            }
        }, ct);
    }
}
