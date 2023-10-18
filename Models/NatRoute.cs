using System.Net.Sockets;

using System.Text;

namespace Brandmauer;

public class NatRoute : Model, IOnDeserialize
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder()
                .AppendBadge("nat", "host", Host, null);
    
            builder.Append("<div class=\"badges\">");
    
            foreach (var translation in Translations)
                builder.AppendBadge(
                    "nat",
                    "translation",
                    $"{translation.Protocol.ToString().ToLower()}: {translation.SourcePort}",
                    $"{translation.TargetPort}"
                );
    
            builder.Append("</div>");
    
            return builder.ToString();
        }
    }

    public bool Enabled { get; set; } = true;

	public Identifier HostReference { get; set; }
    public Host Host;

	public class Translation
	{
		public ProtocolType Protocol { get; set; } = ProtocolType.Tcp;
		public ushort SourcePort { get; set; }
		public ushort TargetPort { get; set; }
    }
	public List<Translation> Translations { get; set; } = new();

    public void OnDeserialize(Database database)
    {
        Host = database.Hosts.FirstOrDefault(x => x.Identifier.Id == HostReference.Id);
    }
}
