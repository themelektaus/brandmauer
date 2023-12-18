using System.Net.Sockets;

namespace Brandmauer;

public partial class Monitor
{
    public string Service_TcpHost { get; set; } = string.Empty;
    public ushort Service_TcpPort { get; set; } = 80;

    bool Check_Service()
    {
        if (Service_TcpHost == string.Empty)
            return true;

        using var tcpClient = new TcpClient
        {
            SendTimeout = 5,
            ReceiveTimeout = 5
        };

        try
        {
            tcpClient.Connect(Service_TcpHost, Service_TcpPort);
        }
        catch
        {
            return false;
        }

        return tcpClient.Connected;
    }
}
