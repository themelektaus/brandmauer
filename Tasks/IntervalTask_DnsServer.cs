using DNS.Client;
using DNS.Server;

namespace Brandmauer;

[Delay(5)]
[Interval(3)]
public class IntervalTask_DnsServer : IntervalTask
{
    DNS.Server.DnsServer dnsServer;
    Task task;

    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

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
