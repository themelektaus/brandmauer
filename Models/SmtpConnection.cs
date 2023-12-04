using System.Text;

namespace Brandmauer;

public class SmtpConnection : Model
{
    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                builder.AppendBadge("smtp", "host", Host, Port);
                builder.AppendBadge("smtp", "username", "User", Username);
                builder.AppendBadge("smtp", "sender", "Sender", Sender);
                builder.AppendBadge(
                    "smtp",
                    Tls ? "tls-enabled" : "tls-disabled",
                    "TLS/SSL",
                    Tls
                        ? "<i class=\"fa-solid fa-check\"></i>"
                        : "<i class=\"fa-solid fa-xmark\"></i>"
                );
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 25;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public bool Tls { get; set; }
}
