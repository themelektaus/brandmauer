namespace Brandmauer;

[Delay(3)]
[Interval(1)]
public class IntervalTask_ReloadDatabase : IntervalTask
{
    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        var lastWriteTime = File.GetLastWriteTime(Database.databaseFile);
        if (Database.LastKnownWriteTime >= lastWriteTime)
            return;

        Database.Load();

        await Task.CompletedTask;
    }

    protected override Task OnDisposeAsync() => default;
}
