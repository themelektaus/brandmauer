namespace Brandmauer;

public class ReverseProxyFeature
{
    public ReverseProxyRoute Route { get; set; }
    public string Source { get; set; }
    public string Target { get; set; }
    public string Suffix { get; set; }
    public bool UseScript { get; set; }
}
