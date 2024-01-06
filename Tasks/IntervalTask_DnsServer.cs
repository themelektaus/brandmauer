using DNS.Client;
using DNS.Server;

namespace Brandmauer;

[Delay(5)]
[Interval(3)]
public class IntervalTask_DnsServer : IntervalTask
{
    DnsServer dnsServer;
    Task task;

    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        if (Database.Use(x => x.Config.EnableDnsServer))
        {
            if (task is null)
                await RestartAsync();

            return;
        }

        if (task is not null)
            await StopAsync();
    }

    protected override async Task OnDisposeAsync() => await StopAsync();

    async ValueTask RestartAsync()
    {
        await StopAsync();

        dnsServer = new(new MasterFile(), "208.67.222.222");

#if DEBUG
        dnsServer.Responded += (sender, e)
            => Audit.Info<DnsServer>($"{e.Request} => {e.Response}");
#endif
        dnsServer.Listening += (sender, e)
            => Audit.Info<DnsServer>("Listening...");

        dnsServer.Errored += (sender, e) =>
        {
            Audit.Error<DnsServer>(e.Exception);

            var error = e.Exception as ResponseException;
            if (error is not null)
                Audit.Error<DnsServer>(error.Response);
        };

        Audit.Info<DnsServer>("Starting...");
        task = dnsServer.Listen();
        Audit.Info<DnsServer>("Started.");
    }

    async Task StopAsync()
    {
        if (dnsServer is not null)
        {
            Audit.Info<DnsServer>("Stopping...");
            try
            {
                dnsServer.Dispose();
                dnsServer = null;
                Audit.Info<DnsServer>("Stopped.");
            }
            catch
            {
                Audit.Error<DnsServer>("Stopping failed.");
            }
        }

        if (task is not null)
        {
            Audit.Info<DnsServer>("Waiting for Task...");
            await task;
            task = null;
            Audit.Info<DnsServer>("Task has finished.");
        }
    }
}
