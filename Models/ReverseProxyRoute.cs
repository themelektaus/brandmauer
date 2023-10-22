using System.Text;

namespace Brandmauer;

public class ReverseProxyRoute : Model, IOnDeserialize
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            var displayNone = SourceHosts.Count == 0 ? " display-none" : "";
            var classes = $"badges badges-top{displayNone}";
            builder.Append($"<div class=\"{classes}\">");

            foreach (var host in SourceHosts)
                builder.AppendBadge(
                    "reverseproxy",
                    "source-host",
                    "Source Host",
                    host
                );

            builder.Append("</div>");

            builder.Append("<div class=\"badges-group\">");

            if (SourceDomains.Count > 0)
            {
                builder.Append("<div class=\"badges\" style=\"flex: 1; \">");

                foreach (var domain in SourceDomains)
                    builder.AppendBadge(
                        "reverseproxy",
                        "source-domain",
                        domain,
                        null
                    );

                builder.Append("</div>");
            }

            if (Target != string.Empty)
            {
                builder.Append("<div class=\"badges-end\">");
                builder.AppendBadge("reverseproxy", "target", "Target", Target);
                builder.Append("</div>");
            }

            builder.Append("</div>");

            return builder.ToString();
        }
    }

    public bool Enabled { get; set; } = true;

    public List<Identifier> SourceHostReferences { get; set; } = new();
    public List<Host> SourceHosts;

    public List<StringValue> SourceDomains { get; set; } = new();

    public string Target { get; set; } = string.Empty;

    public bool UseYarp { get; set; } = false;
    public bool KeepHost { get; set; } = false;

    public void OnDeserialize(Database database)
    {
        SourceHosts = database.Hosts
            .Where(x => SourceHostReferences.Any(y => y.Id == x.Identifier.Id))
            .ToList();
    }
}
