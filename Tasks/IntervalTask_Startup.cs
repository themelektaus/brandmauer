#if LINUX
namespace Brandmauer;

[OneShot]
[Delay(15)]
public class IntervalTask_Startup : IntervalTask
{
    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override Task OnTickAsync()
    {
        Endpoint.Build.ApplyInternal(clear: false);

        return default;
    }

    protected override Task OnDisposeAsync() => default;
}
#endif
