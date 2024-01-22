using System.Text;

namespace Brandmauer;

public partial class DynamicDnsHost : Model, IAsyncUpdateable
{
    public bool Enabled { get; set; } = true;

    public int Interval { get; set; } = 300;

    public enum _IpResolver
    {
        None,
        IfConfigMe,
        IfConfigCo
    }
    public _IpResolver IpResolver { get; set; }

    public enum _Provider
    {
        None,
        NoIp,
        NameCom
    }

    public _Provider Provider { get; set; }

    public class _Provider_NoIp
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
    }
    public _Provider_NoIp Provider_NoIp { get; set; } = new();

    public class _Provider_NameCom
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public int RecordId { get; set; }
    }
    public _Provider_NameCom Provider_NameCom { get; set; } = new();

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                switch (Provider)
                {
                    case _Provider.NoIp:
                        builder.AppendBadge(
                            "dynamicdnshost",
                            "provider",
                            "noip.com",
                            Provider_NoIp.Hostname
                        );
                        break;

                    case _Provider.NameCom:
                        builder.AppendBadge(
                            "dynamicdnshost",
                            "provider",
                            "name.com",
                            Provider_NameCom.Domain
                        );
                        break;
                }
                
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    DateTime? last;

    public bool ShouldUpdate => Enabled;

    public async Task UpdateAsync()
    {
        if (Interval <= 0)
            return;

        var now = DateTime.Now;

        if (last is not null)
            if ((now - last.Value).TotalSeconds < Interval)
                return;

        last = now;

        string ip = null;

        switch (IpResolver)
        {
            case _IpResolver.None:
                break;
            case _IpResolver.IfConfigMe:
                ip = await GetIpAsync_IfConfigMe();
                break;
            case _IpResolver.IfConfigCo:
                ip = await GetIpAsync_IfConfigCo();
                break;
        }

        if (ip is null)
            return;

        switch (Provider)
        {
            case _Provider.None:
                break;
            case _Provider.NoIp:
                await UpdateAsync_NoIp(ip);
                break;
            case _Provider.NameCom:
                await UpdateAsync_NameCom(ip);
                break;
        }
    }

    static async Task<string> GetIpAsync_IfConfigMe()
    {
        using var httpClient = new HttpClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://ifconfig.me"
        );

        request.Headers.Add("User-Agent", "curl");
        request.Headers.Add("Referer", Utils.Name.ToLower());

        using var response = await httpClient.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    static async Task<string> GetIpAsync_IfConfigCo()
    {
        using var httpClient = new HttpClient();

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://ifconfig.co"
        );

        request.Headers.Add("User-Agent", "curl");

        using var response = await httpClient.SendAsync(request);
        return (await response.Content.ReadAsStringAsync()).TrimEnd();
    }

    async Task UpdateAsync_NoIp(string ip)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "http://dynupdate.no-ip.com/nic/update" +
            $"?hostname={Provider_NoIp.Hostname}" +
            $"&myip={ip}"
        );

        var username = Provider_NoIp.Username;
        var password = Provider_NoIp.Password;

        request.Headers.Authorization = new(
            "Basic",
            $"{username}:{password}".ToBase64()
        );

#if DEBUG
        var requestInfo = RequestInfo.Create(request, string.Empty);
#endif

        using var httpClient = new HttpClient();
        using var response = await httpClient.SendAsync(request);

#if DEBUG
        RequestInfo.Add(requestInfo, response, response.Content.ReadAsStream());
#endif
    }

    async Task UpdateAsync_NameCom(string ip)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "https://api.name.com/v4/domains/" +
            $"{Provider_NameCom.Domain}/records/{Provider_NameCom.RecordId}"
        );

        var username = Provider_NameCom.Username;
        var password = Provider_NameCom.Password;

        request.Headers.Authorization = new(
            "Basic",
            $"{username}:{password}".ToBase64()
        );

        var content = @$"{{""answer"":""{ip}""}}";
        request.Content = new StringContent(content);

#if DEBUG
        var requestInfo = RequestInfo.Create(request, content);
#endif

        using var httpClient = new HttpClient();
        using var response = await httpClient.SendAsync(request);

#if DEBUG
        RequestInfo.Add(requestInfo, response, response.Content.ReadAsStream());
#endif
    }
}
