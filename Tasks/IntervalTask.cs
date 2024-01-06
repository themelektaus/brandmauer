using System.Reflection;

namespace Brandmauer;

public abstract class IntervalTask : IAsyncDisposable
{
    protected abstract Task OnStartAsync();
    protected abstract Task OnBeforeFirstTickAsync();
    protected abstract Task OnTickAsync();
    protected abstract Task OnDisposeAsync();

    readonly string name;
    readonly CancellationTokenSource ctSource;
    readonly CancellationToken ctToken;

    readonly TimeSpan delay;
    readonly TimeSpan interval;

    protected bool IsDisposed() => ctSource.IsCancellationRequested;

    public IntervalTask()
    {
        var t = GetType();

        name = t.Name;
        ctSource = new();
        ctToken = ctSource.Token;

        delay = TimeSpan.FromSeconds(
            t.GetCustomAttribute<DelayAttribute>()?.seconds ?? 0
        );

        interval = TimeSpan.FromSeconds(
            t.GetCustomAttribute<IntervalAttribute>()?.seconds ?? 1
        );
    }

    public void RunInBackground()
    {
        Task.Run(async () =>
        {
            Audit.Info<IntervalTask>($"{name} is starting...");
            await (OnStartAsync() ?? Task.CompletedTask);

            try { await Task.Delay(delay, ctToken); } catch { }

            await (OnBeforeFirstTickAsync() ?? Task.CompletedTask);

            Audit.Info<IntervalTask>($"{name} has started.");

            while (!IsDisposed())
            {
                await (OnTickAsync() ?? Task.CompletedTask);
                try { await Task.Delay(interval, ctToken); } catch { }
            }

            Audit.Info<IntervalTask>($"{name} has finished.");
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (IsDisposed())
        {
            Audit.Info<IntervalTask>(
                $"{name} has already been disposed. Skipping..."
            );
            return;
        }

        Audit.Info<IntervalTask>($"Disposing {name}...");

        try
        {
            await (OnDisposeAsync() ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            Audit.Warning<IntervalTask>(ex.Message);
        }

        ctSource.Cancel();

        Audit.Info<IntervalTask>($"{name} has been disposed.");
    }
}
