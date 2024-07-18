namespace Brandmauer.LiveCode;

public class Host
{
    public event EventHandler<HostReceiveEventArgs> OnReceive;

    public void Send(params object[] data)
    {
        OnReceive?.Invoke(this, new HostReceiveEventArgs(data));
    }
}
