namespace Brandmauer.LiveCode;

public class HostReceiveEventArgs(object[] data) : System.EventArgs
{
    public readonly object[] data = data;
}
