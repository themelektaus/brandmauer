namespace Brandmauer;

public class PushChecker : IntervalTask
{
    protected override TimeSpan Delay => TimeSpan.FromSeconds(10);
    protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

    protected override Task OnStartAsync()
    {
        Database.Use(x =>
        {
            x.PushListeners.ForEach(y => y.Touch());
            x.Save();
        });

        return Task.CompletedTask;
    }

    protected override Task OnBeforeFirstTickAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task OnTickAsync()
    {
        var (pushers, pushListeners) = Database.Use(
            x => (
                x.Pushers.Where(x => x.Enabled),
                x.PushListeners
                 .Where(x => x.Enabled && x.AgeThreshold > 0).ToList()
            )
        );

        foreach (var pusher in pushers)
            await pusher.UpdateAsync();

        foreach (var pushListener in pushListeners)
            await pushListener.UpdateAsync();
    }

    protected override Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }
}
