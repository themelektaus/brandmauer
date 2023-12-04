using MailKitSmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Brandmauer;

public class SmtpClient
{
    public string host = "localhost";
    public int port = 25;
    public string username;
    public string password;
    public string from;
    public List<string> to = new();
    public List<string> cc = new();
    public List<string> bcc = new();
    public string subject;
    public string body;
    public bool tls = false;
    public bool ssl = false;
    public bool html = false;

    public async Task<(string result, System.Net.Security.SslPolicyErrors sslPolicyErrors)> Send()
    {
        var builder = new MimeKit.BodyBuilder();

        if (html)
            builder.HtmlBody = body;
        else
            builder.TextBody = body;

        using var message = new MimeKit.MimeMessage();

        message.From.Add(GetMailboxAddress(from));
        message.To.AddRange(to.Select(GetMailboxAddress));
        message.Cc.AddRange(cc.Select(GetMailboxAddress));
        message.Bcc.AddRange(bcc.Select(GetMailboxAddress));
        message.Subject = subject;
        message.Body = builder.ToMessageBody();

        using var client = new MailKitSmtpClient();

        var sslPolicyErrors = System.Net.Security.SslPolicyErrors.None;
        client.ServerCertificateValidationCallback = (_, _, _, _sslPolicyErrors) =>
        {
            sslPolicyErrors = _sslPolicyErrors;
            return true;
        };

        if (ssl)
            await client.ConnectAsync(host, port, true);
        else if (tls)
            await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
        else
            await client.ConnectAsync(host, port);

        if (username is not null && password is not null)
            await client.AuthenticateAsync(username, password);

        var result = await client.SendAsync(message);
        await client.DisconnectAsync(true);
        return (result, sslPolicyErrors);
    }

    static MimeKit.MailboxAddress GetMailboxAddress(string fullAddress)
    {
        var (name, address) = GetMailAddress(fullAddress);
        return new(name, address);
    }

    public static (string name, string address) GetMailAddress(string fullAddress)
    {
        var x = fullAddress?.Split('<', 2);

        if (x.Length < 2)
            return (fullAddress, fullAddress);

        return (x[0].Trim(), x[1].Trim().TrimEnd('>').TrimEnd());
    }
}
