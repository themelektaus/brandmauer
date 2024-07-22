using System.Text;

using SslPolicyErrors = System.Net.Security.SslPolicyErrors;

namespace Brandmauer;

public class SmtpConnection : Model
{
    public override string ShortName
    {
        get
        {
            if (Name != string.Empty)
                return Name;

            return $"{Username} ({Host}:{Port})";
        }
    }

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                builder.AppendBadge("smtp", "host", Host, Port);

                if (Username != string.Empty)
                    builder.AppendBadge("smtp", "username", "User", Username);

                if (Sender != string.Empty)
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

    public async Task<(int statusCode, string text)> SendAsync(
        List<string> to,
        string subject,
        string body,
        bool html
    )
    {
        var client = new SmtpClient
        {
            host = Host,
            port = Port,
            username = Username,
            password = Password,
            from = Sender == string.Empty ? Username : Sender,
            to = to,
            subject = subject,
            body = body,
            tls = Tls,
            html = html
        };

        var result = new StringBuilder();

        result.AppendLine("-- Begin client options --");
        result.AppendLine($"Host: {client.host}");
        result.AppendLine($"Port: {client.port}");
        result.AppendLine($"TLS/SSL: {(client.tls ? "Yes" : "No")}");
        result.AppendLine($"From: {client.from}");
        result.AppendLine($"To: {client.to.Join(", ")}");
        result.AppendLine($"Subject: {client.subject}");
        result.AppendLine($"Body: {client.body}");
        result.AppendLine("-- End client options --");

        var error = false;

        try
        {
            var response = await client.Send();

            if (response.sslPolicyErrors == SslPolicyErrors.None)
            {
                result.AppendLine("[OK] Success");
            }
            else
            {
                result.AppendLine("[WARNING] Success, with SSL Policy Errors");
                result.AppendLine(response.sslPolicyErrors.ToString());
            }

            result.AppendLine(response.result);
        }
        catch (Exception ex)
        {
            result.AppendLine("[ERROR]");
            result.AppendLine(ex.Message);
            error = true;
        }

        var text = result.ToString();
        return (error ? 400 : 200, text);
    }
}
