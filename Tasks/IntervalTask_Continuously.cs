namespace Brandmauer;

[Delay(10)]
[Interval(1)]
public class IntervalTask_Continuously : IntervalTask
{
    protected override Task OnStartAsync()
    {
        Database.Use(x =>
        {
            x.PushListeners.ForEach(y => y.Touch());
            x.Save(logging: false);
        });

        return default;
    }

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        await Database.UpdateAsync();
    }

    protected override Task OnDisposeAsync() => default;
}
