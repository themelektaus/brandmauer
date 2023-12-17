using System.Net.Sockets;

namespace Brandmauer;

public class Pusher : Model
{
    public bool Enabled { get; set; } = true;

    public int Interval { get; set; } = 1;

    public string TcpHost { get; set; } = string.Empty;
    public ushort TcpPort { get; set; } = 1;

    public string SuccessUrl { get; set; } = string.Empty;
    public List<StringValue> SuccessHeaders { get; set; } = new();

    DateTime? last;

    public void Reset()
    {
        last = null;
    }

    public async Task UpdateAsync()
    {
        var now = DateTime.Now;
        if (last is not null && (now - last.Value).TotalSeconds < Interval)
            return;

        last = now;

        using var tcpClient = new TcpClient();

        try { tcpClient.Connect(TcpHost, TcpPort); } catch { return; }
        if (!tcpClient.Connected)
            return;

        using var httpClient = new HttpClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, SuccessUrl);
        foreach (var header in SuccessHeaders)
            request.Headers.Add(header.Value, header.Description);

        try { await httpClient.SendAsync(request); } catch { }
    }
}
