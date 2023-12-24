namespace Brandmauer;

[Delay(10)]
[Interval(1)]
public class IntervalTask_Push : IntervalTask
{
    protected override Task OnStartAsync()
    {
        Database.Use(x =>
        {
            x.PushListeners.ForEach(y => y.Touch());
            x.Save(logging: false);
        });

        return Task.CompletedTask;
    }

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        var (monitors, pushListeners) = Database.Use(
            x => (
                x.Monitors
                    .Where(x => x.Enabled)
                    .ToList(),
                x.PushListeners
                    .Where(x => x.Enabled && x.AgeThreshold > 0)
                    .ToList()
            )
        );

        foreach (var pusher in monitors)
            await pusher.UpdateAsync();

        foreach (var pushListener in pushListeners)
            await pushListener.UpdateAsync();
    }

    protected override Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }
}
