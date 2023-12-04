namespace Brandmauer;

public class Config : Model
{
    public string LetsEncryptAccountMailAddress { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string LastBuild { get; set; }
    public bool EnableDnsServer { get; set; }
}
