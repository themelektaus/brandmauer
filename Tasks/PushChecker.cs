namespace Brandmauer;

public class PushChecker : IntervalTask
{
    protected override TimeSpan Delay => TimeSpan.FromSeconds(30);
    protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

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
        var pushListeners = Database.Use(
            x => x.PushListeners
                .Where(x => x.Enabled && x.AgeThreshold > 0)
                .ToList()
        );

        foreach (var pushListener in pushListeners)
            await pushListener.UpdateAsync();
    }

    protected override Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }
}
