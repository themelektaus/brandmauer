using System.Text;

namespace Brandmauer;

public class ReverseProxyRoute : Model, IOnDeserialize
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges("flex: 4; ");
            {
                if (Name != string.Empty)
                    builder.AppendBadge("reverseproxy", "name", "Name", Name);

                foreach (var host in SourceHosts)
                    builder.AppendBadge(
                        "reverseproxy",
                        "source-host",
                        "Source Host",
                        host
                    );

                if (SourceDomains.Count > 0)
                {
                    foreach (var domain in SourceDomains)
                        builder.AppendBadge(
                            "reverseproxy",
                            "source-domain",
                            domain,
                            null
                        );
                }
            }
            builder.EndBadges();

            builder.BeginBadges("flex: 3; ");
            {
                if (Target != string.Empty)
                {
                    builder.AppendBadge(
                        "reverseproxy",
                        "target",
                        "Target",
                        Target
                    );
                }

                if (UseYarp)
                    builder.AppendBadge("reverseproxy", "yarp", "YARP", null);

                if (HostModification != _HostModification.Target)
                    builder.AppendBadge(
                        "reverseproxy",
                        "host-modification",
                        "Host Modification",
                        HostModification.ToString()
                    );
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public bool Enabled { get; set; } = true;

    public List<Identifier> SourceHostReferences { get; set; } = new();
    public List<Host> SourceHosts;

    public List<StringValue> SourceDomains { get; set; } = new();

    public string Target { get; set; } = string.Empty;

    public bool UseTeapot { get; set; } = false;
    public bool UseYarp { get; set; } = false;

    public enum _HostModification
    {
        Unset = -2,
        Origin = -1,
        Target = 0,
        Null = 1
    }
    public _HostModification HostModification { get; set; }
        = _HostModification.Target;

    public MemoryValue MaxBodySize { get; set; }
        = new() { Unit = MemoryValue._Unit.Default };

    public void OnDeserialize(Database database)
    {
        SourceHosts = database.Hosts
            .Where(x => SourceHostReferences.Any(y => y.Id == x.Identifier.Id))
            .ToList();
    }
}
