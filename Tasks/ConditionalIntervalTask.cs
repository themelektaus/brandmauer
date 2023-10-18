namespace Brandmauer;

public abstract class ConditionalIntervalTask : IntervalTask
{
	protected abstract bool ShouldTrigger();
	protected abstract Task OnTriggerAsync();

	protected sealed override async Task OnTickAsync()
	{
		if (ShouldTrigger())
			await OnTriggerAsync();
	}
}
