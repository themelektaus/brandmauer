using DNS.Client;
using DNS.Server;

namespace Brandmauer;

public class DnsServer : IntervalTask
{
    DNS.Server.DnsServer dnsServer;
    Task task;

    protected override TimeSpan Delay => TimeSpan.FromSeconds(5);

    protected override TimeSpan Interval => TimeSpan.FromSeconds(3);

    protected override Task OnStartAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task OnBeforeFirstTickAsync()
    {
        return Task.CompletedTask;
    }

    protected override async Task OnTickAsync()
    {
        if (!disposed && Database.Use(x => x.Config.EnableDnsServer))
        {
            if (task is null)
                await RestartAsync();

            return;
        }

        if (task is not null)
            await DisposeAsync();
    }

    async ValueTask RestartAsync()
    {
        await DisposeAsync();

        dnsServer = new(new MasterFile(), "208.67.222.222");

        //208.67.220.220
        //1.1.1.1
        //8.8.8.8
        //192.168.0.60

#if DEBUG
        dnsServer.Responded += (sender, e) => Utils.Log("DNS", $"{e.Request} => {e.Response}");
#endif
        dnsServer.Listening += (sender, e) => Utils.Log("DNS", $"Listening...");
        dnsServer.Errored += (sender, e) =>
        {
            Utils.Log("DNS-Error", e.Exception.ToString());

            var error = e.Exception as ResponseException;
            if (error is not null)
                Utils.Log("DNS-Error", error.Response.ToString());
        };

        task = dnsServer.Listen();
    }

    protected override async Task OnDisposeAsync()
    {
        if (dnsServer is not null)
        {
            dnsServer.Dispose();
            dnsServer = null;
        }

        if (task is not null)
        {
            await task;
            task = null;
        }
    }
}
