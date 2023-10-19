namespace Brandmauer;

public class IpTablesNat
{
    public string name;
    public string source;
    public string protocol;
    public string destinationPort;
    public string destination;
    public bool enabled;

    public readonly List<string> errors = new();

    IpTablesNat() { }

    public static List<IpTablesNat> From(IList<Rule> rules, NatRoute natRoule)
    {
        var preroutings = new List<IpTablesNat>();

        var basePrerouting = new IpTablesNat
        {
            name = natRoule.Name
        };

        if (natRoule.Host is null)
        {
            basePrerouting.errors.Add("host is null");
            preroutings.Add(basePrerouting);
            goto Return;
        }

        if (natRoule.Host.Addresses.Count != 1)
        {
            basePrerouting.errors.Add("one destination per rule");
            preroutings.Add(basePrerouting);
            goto Return;
        }

        if (!Utils.TryGetIpAddress(natRoule.Host.Addresses[0].Value, out var destinationIpAddress))
        {
            basePrerouting.errors.Add("destination is not a ip address");
            preroutings.Add(basePrerouting);
            goto Return;
        }

        basePrerouting.enabled = natRoule.Enabled;

        foreach (var translation in natRoule.Translations)
        {
            List<string> sources = null;

            foreach (var rule in rules)
            {
                if (!rule.IsAllowed(translation))
                    continue;

                sources ??= new();

                if (!rule.TryGetSource(out var source))
                    continue;

                if (source is null)
                {
                    sources.Clear();
                    break;
                }

                sources.Add(source);
            }

            if (sources is null)
                continue;

            var prerouting = new IpTablesNat
            {
                enabled = basePrerouting.enabled
            };

            if (sources.Count > 0)
                prerouting.source = sources.Join(',');

            prerouting.protocol = translation.Protocol.ToString().ToLower();

            if (translation.Protocol == System.Net.Sockets.ProtocolType.Icmp)
            {
                prerouting.destination = destinationIpAddress;
            }
            else
            {
                prerouting.destinationPort = translation.SourcePort.ToString();
                prerouting.destination = $"{destinationIpAddress}:{translation.TargetPort}";
            }

            preroutings.Add(prerouting);
        }

    Return:
        return preroutings;
    }

    public override string ToString()
    {
        if (errors.Count > 0)
            return $"# ERROR: {errors.Join(", ")}";

        var args = new List<string>();

        var e = $"{(enabled ? "" : "#")}";

        args.Add($"{e} {IpTablesBuilder.IPTABLES}");
        args.Add("-t nat");
        args.Add("-A PREROUTING");
        args.Add($"-p {protocol}");
        if (protocol != "icmp")
            args.Add($"-m {protocol}");
        args.Add($"\\\n   {e} ");
        args.AddIfNotNull("-s ", source);
        args.AddIfNotNull("--dport ", destinationPort);
        args.Add("-j DNAT");
        args.Add($"\\\n   {e} ");
        args.Add($"--to-destination {destination}\n   \n  ");

        args.Add($"{e} {IpTablesBuilder.IPTABLES}");
        args.Add("-t nat");
        args.Add("-A OUTPUT");
        args.Add($"-p {protocol}");
        if (protocol != "icmp")
            args.Add($"-m {protocol}");
        args.Add($"\\\n   {e} ");
        args.AddIfNotNull("--dport ", destinationPort);
        args.Add("-j DNAT");
        args.Add($"\\\n   {e} ");
        args.Add($"--to-destination {destination}");

        return args.Join(' ');
    }
}
