namespace Brandmauer;

public class Config : Model
{
    public string ExternalUrl { get; set; } = string.Empty;
    public string LetsEncryptAccountMailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LastBuild { get; set; }
    public bool EnableDnsServer { get; set; }
    public bool EnableFortiClient { get; set; }
}
