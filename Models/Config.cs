namespace Brandmauer;

public class Config : Model
{
	public string LetsEncryptAccountMailAddress { get; set; } = string.Empty;

	public bool ReverseProxyLogging { get; set; } = false;

	public string Notes { get; set; } = string.Empty;

    public string LastBuild { get; set; }
}
