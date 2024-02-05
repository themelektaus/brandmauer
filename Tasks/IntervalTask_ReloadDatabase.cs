namespace Brandmauer;

[Delay(3)]
[Interval(1)]
public class IntervalTask_ReloadDatabase : IntervalTask
{
    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override Task OnTickAsync()
    {
        var lastWriteTime = File.GetLastWriteTime(Database.databaseFile);

        if (Database.LastKnownWriteTime < lastWriteTime)
            Database.Load();

        return default;
    }

    protected override Task OnDisposeAsync() => default;
}
