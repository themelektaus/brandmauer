using System.Net.Sockets;

using System.Text;

namespace Brandmauer;

public class Service : Model
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            foreach (var port in Ports)
                builder.AppendBadge("service", "port", port, null);

            return builder.ToString();
        }
    }

    public class Port
    {
        public enum _Area { None, Single, Range, Any }
        public _Area Area { get; set; } = _Area.Single;

        public ushort Start { get; set; } = 1;
        public ushort End { get; set; } = ushort.MaxValue;

        public ProtocolType Protocol { get; set; } = ProtocolType.Tcp;

        public string GetRange(char separator) => Area switch
        {
            _Area.None => null,
            _Area.Single => Start.ToString(),
            _Area.Range => Start.ToString() + separator + End,
            _ => "",
        };

        public override string ToString()
        {
            var result = Area switch
            {
                _Area.None => "-",
                _Area.Single => Start.ToString(),
                _Area.Range => Start.ToString() + ":" + End,
                _ => "any",
            };
            return $"{result} <span>{Protocol.ToString().ToLower()}</span>";
        }
    }

    public List<Port> Ports { get; set; } = new();
}
