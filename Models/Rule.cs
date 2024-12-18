using System.Text;

namespace Brandmauer;

public class Rule : Model, IOnDeserialize
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder()
                .AppendBadge("rule", "direction", Direction, Action);

            builder.BeginBadges();
            {
                if (Hosts.Count == 0)
                    builder.AppendBadge("rule", "host", "Host", "any");
                else
                    foreach (var host in Hosts.Select(x => x.ToString()))
                        builder.AppendBadge("rule", "host", "Host", host);
            }
            builder.EndBadges();

            builder.BeginBadges();
            {
                if (Services.Count == 0)
                    builder.AppendBadge("rule", "service", "none", null);
                else
                    foreach (var service in Services.Select(x => x.ToString()))
                        builder.AppendBadge("rule", "service", service, null);
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public bool Enabled { get; set; } = true;

    public enum _Direction { None = 0, Inbound = 1, Outbound = 2 }
    public _Direction Direction { get; set; } = _Direction.Inbound;

    public enum _Action { None = 0, Allow = 1, Block = 2 }
    public _Action Action { get; set; } = _Action.Allow;

    public List<Identifier> HostReferences { get; set; } = new();
    public List<Host> Hosts;

    public List<Identifier> ServiceReferences { get; set; } = new();
    public List<Service> Services;

    public void OnDeserialize(Database database)
    {
        Hosts = database.Hosts.Where(
            x => HostReferences.Any(
                y => y.Id == x.Identifier.Id
            )
        ).ToList();

        Services = database.Services.Where(
            x => ServiceReferences.Any(
                y => y.Id == x.Identifier.Id
            )
        ).ToList();
    }

    public bool TryGetSource(out string sourceArg)
    {
        if (Hosts.Count == 0)
        {
            sourceArg = null;
            return true;
        }

        var sourceHosts = Hosts
            .Where(x => x is not null)
            .SelectMany(x => x.Addresses)
            .SelectMany(x => x.Value.ToIpAddresses())
            .Distinct()
            .ToList();

        if (sourceHosts.Count == 0)
        {
            sourceArg = null;
            return false;
        }

        sourceArg = sourceHosts.Join(',');
        return true;
    }

    public bool IsAllowed(NatRoute.Translation translation)
    {
        if (!Enabled)
            return false;

        if (Direction != _Direction.Inbound)
            return false;

        if (Action != _Action.Allow)
            return false;

        foreach (var service in Services)
        {
            foreach (var servicePort in service.Ports)
            {
                if (servicePort.Protocol != translation.Protocol)
                    continue;

                if (servicePort.Area == Service.Port._Area.Any)
                    return true;

                if (servicePort.Area == Service.Port._Area.Single)
                    if (servicePort.Start == translation.SourcePort)
                        return true;

                if (servicePort.Area == Service.Port._Area.Range)
                    if (servicePort.Start <= translation.SourcePort)
                        if (translation.SourcePort <= servicePort.End)
                            return true;
            }
        }

        return false;
    }
}
