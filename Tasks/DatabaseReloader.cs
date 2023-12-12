namespace Brandmauer;

public class DatabaseReloader : ConditionalIntervalTask
{
    protected override TimeSpan Delay => TimeSpan.FromSeconds(3);
    protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

    protected override Task OnStartAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task OnBeforeFirstTickAsync()
    {
        return Task.CompletedTask;
    }

    protected override bool ShouldTrigger()
    {
        var lastWriteTime = File.GetLastWriteTime(Database.databaseFile);
        return Database.LastKnownWriteTime < lastWriteTime;
    }

    protected override async Task OnTriggerAsync()
    {
        Database.Load();

        await Task.CompletedTask;
    }

    protected override Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }
}
