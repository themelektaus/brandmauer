using System.Text;

namespace Brandmauer;

public class Host : Model
{
    public List<StringValue> Addresses { get; set; } = new();

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                foreach (var address in Addresses)
                    builder.AppendBadge("host", "address", address, null);
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }
}
