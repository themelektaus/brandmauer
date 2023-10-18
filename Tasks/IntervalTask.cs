namespace Brandmauer;

public abstract partial class IntervalTask : IAsyncDisposable
{
	protected abstract TimeSpan Delay { get; }
	protected abstract TimeSpan Interval { get; }

	protected abstract Task OnTickAsync();

	Task task;

	bool disposed;

	public async ValueTask DisposeAsync()
	{
		disposed = true;

		if (task is not null)
		{
			await task;
			task = null;
		}
	}

	public void RunInBackground()
	{
		task = Task.Run(async () =>
		{
			await Task.Delay(Delay);

			while (!disposed)
			{
				await OnTickAsync();
				await Task.Delay(Interval);
			}
		});
	}
}
