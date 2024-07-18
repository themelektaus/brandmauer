namespace Brandmauer;

public class ReverseProxyFeature
{
    public ReverseProxyRoute Route { get; set; }
    public string Domain { get; set; }
    public string Target { get; set; }
    public string Suffix { get; set; }
    public bool UseScript { get; set; }
}
