using DNS.Client;
using DNS.Protocol;
using DNS.Protocol.ResourceRecords;

using System.Net;

namespace Brandmauer;

public static class DnsUtils
{
    static readonly string[] publicResolvers = ["1.1.1.1", "8.8.8.8"];

    const int port = 53;

    public static async Task<List<IPAddress>> GetAuthoritativeServersAsync(
        string zone
    )
    {
        var result = new List<IPAddress>();

        foreach (var resolver in publicResolvers)
        {
            List<string> names;

            try
            {
                var client = new DnsClient(resolver, port);
                var response = await client.Resolve(zone, RecordType.NS);

                names = response.AnswerRecords
                    .Concat(response.AuthorityRecords)
                    .OfType<NameServerResourceRecord>()
                    .Select(x => x.NSDomainName.ToString())
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                Audit.Warning(
                    typeof(DnsUtils),
                    $"NS lookup for \"{zone}\" via {resolver} failed: "
                        + ex.Message
                );
                continue;
            }

            foreach (var name in names)
            {
                try
                {
                    var client = new DnsClient(resolver, port);
                    var addresses = await client.Lookup(name, RecordType.A);
                    result.AddRange(addresses);
                }
                catch (Exception ex)
                {
                    Audit.Warning(
                        typeof(DnsUtils),
                        $"Resolving nameserver \"{name}\" failed: {ex.Message}"
                    );
                }
            }

            if (result.Count > 0)
                break;
        }

        return result.Distinct().ToList();
    }

    public static async Task<bool> WaitForTxtRecordsAsync(
        string zone,
        string name,
        ICollection<string> expected,
        TimeSpan timeout
    )
    {
        var servers = await GetAuthoritativeServersAsync(zone);

        if (servers.Count == 0)
        {
            Audit.Warning(
                typeof(DnsUtils),
                $"No authoritative nameserver found for \"{zone}\". "
                    + "Falling back to public resolvers."
            );

            servers = publicResolvers.Select(IPAddress.Parse).ToList();
        }

        Audit.Info(
            typeof(DnsUtils),
            $"Waiting for {expected.Count} TXT record(s) at \"{name}\" on "
                + $"{servers.Count} nameserver(s): {string.Join(", ", servers)}"
        );

        var deadline = DateTime.UtcNow + timeout;

        while (true)
        {
            var pending = new List<string>();

            foreach (var server in servers)
            {
                var values = await GetTxtRecordsAsync(server, name);

                if (expected.All(values.Contains))
                    continue;

                pending.Add(server.ToString());
            }

            if (pending.Count == 0)
            {
                Audit.Info(
                    typeof(DnsUtils),
                    $"TXT record(s) at \"{name}\" have propagated."
                );
                return true;
            }

            if (DateTime.UtcNow >= deadline)
            {
                Audit.Error(
                    typeof(DnsUtils),
                    $"TXT record(s) at \"{name}\" did not propagate within "
                        + $"{timeout.TotalSeconds:0} seconds. Still missing "
                        + $"on: {string.Join(", ", pending)}"
                );
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    static async Task<List<string>> GetTxtRecordsAsync(
        IPAddress server,
        string name
    )
    {
        try
        {
            var client = new DnsClient(server, port);
            var response = await client.Resolve(name, RecordType.TXT);

            return response.AnswerRecords
                .OfType<TextResourceRecord>()
                .Select(x => x.ToStringTextData())
                .ToList();
        }
        catch
        {
            return [];
        }
    }
}
