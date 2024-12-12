#if LINUX
namespace Brandmauer;

[Delay(60)]
[Interval(60)]
public class IntervalTask_FortiClient : IntervalTask
{
    bool? active;

    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        if (Database.Use(x => x.Config.EnableFortiClient))
        {
            if (active is null || !active.HasValue)
            {
                active = true;

                ShellCommand.Execute("bash /root/ck-connect.sh");
            }
            else
            {
                ShellCommand.Execute("bash /root/ck-update.sh");
            }

            return;
        }

        if (active is null || active.HasValue)
        {
            active = false;

            ShellCommand.Execute("bash /root/ck-disconnect.sh");
        }

        await Task.CompletedTask;
    }

    protected override async Task OnDisposeAsync()
    {
        if (active is not null && active.HasValue)
        {
            ShellCommand.Execute("bash /root/ck-disconnect.sh");
        }

        await Task.CompletedTask;
    }
}
#endif
