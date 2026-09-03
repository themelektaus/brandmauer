using DNS.Client;
using DNS.Server;

namespace Brandmauer;

[Delay(5)]
[Interval(3)]
public class IntervalTask_DnsServer : IntervalTask
{
    DnsServer dnsServer;
    Task task;

#if DEBUG
    EventHandler<DnsServer.RespondedEventArgs> respondedHandler;
#endif
    EventHandler<EventArgs> listeningHandler;
    EventHandler<DnsServer.ErroredEventArgs> erroredHandler;

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
        respondedHandler = (sender, e)
            => Audit.Info<DnsServer>($"{e.Request} => {e.Response}");
        dnsServer.Responded += respondedHandler;
#endif

        listeningHandler = (sender, e)
            => Audit.Info<DnsServer>("Listening...");
        dnsServer.Listening += listeningHandler;

        erroredHandler = (sender, e) =>
        {
            Audit.Error<DnsServer>(e.Exception);

            var error = e.Exception as ResponseException;
            if (error is not null)
                Audit.Error<DnsServer>(error.Response);
        };
        dnsServer.Errored += erroredHandler;

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
#if DEBUG
                if (respondedHandler is not null)
                    dnsServer.Responded -= respondedHandler;
#endif
                if (listeningHandler is not null)
                    dnsServer.Listening -= listeningHandler;
                if (erroredHandler is not null)
                    dnsServer.Errored -= erroredHandler;

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
