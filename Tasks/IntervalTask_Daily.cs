namespace Brandmauer;

[Delay(43200)]
[Interval(86400)]
public class IntervalTask_Daily : IntervalTask
{
    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override Task OnTickAsync()
    {
        Audit.CleanUp();

        return default;
    }

    protected override Task OnDisposeAsync() => default;
}
