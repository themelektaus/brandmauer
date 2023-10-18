namespace Brandmauer;

public class DatabaseReloader : ConditionalIntervalTask
{
	protected override TimeSpan Delay => TimeSpan.FromSeconds(3);
	protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

	protected override bool ShouldTrigger()
	{
		return Database.LastKnownWriteTime < File.GetLastWriteTime(Database.databaseFile);
	}

	protected override async Task OnTriggerAsync()
	{
		Database.Load();

        await Task.CompletedTask;
	}
}
