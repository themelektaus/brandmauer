using DNS.Client;
using DNS.Server;

using _DnsServer = DNS.Server.DnsServer;

namespace Brandmauer;

public class DnsServer
{
    readonly MasterFile masterFile;
    readonly _DnsServer dnsServer;

    Task task;

    public DnsServer()
    {
        masterFile = new();
        dnsServer = new _DnsServer(masterFile, "208.67.222.222");

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
    }

    public void Start()
    {
        task = dnsServer.Listen();
    }

    public async Task StopAsync()
    {
        dnsServer.Dispose();
        await task;
    }
}
