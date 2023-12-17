using System.Text;

namespace Brandmauer;

public class Authentication : Model, IOnDeserialize
{
    public override string ShortName
    {
        get
        {
            if (Name != string.Empty)
                return Name;

            if (Username != string.Empty)
                return Username;

            return Identifier.Id.ToString();
        }
    }

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                builder.AppendBadge(
                    "authentication",
                    "username",
                    Username,
                    null
                );

                builder.AppendBadge(
                    "authentication",
                    EnableTotp ? "totp-enabled" : "totp-disabled",
                    "TOTP",
                    EnableTotp
                        ? "<i class=\"fa-solid fa-check\"></i>"
                        : "<i class=\"fa-solid fa-xmark\"></i>"
                );
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableTotp { get; set; }
    public string TotpSecret { get; set; } = TotpUtils.GenerateSecret();

    public string TotpUrl;
    public string TotpQrCode;

    public void OnDeserialize(Database _)
    {
        TotpUrl = TotpUtils.GenerateUrl(Username, TotpSecret);

        var data = TotpUtils.GenerateQrCode(TotpUrl).ToBase64();
        TotpQrCode = $"data:image/png;base64,{data}";
    }

    public List<string> GetAuthorizations()
    {
        var authorizations = new List<string>();

        if (EnableTotp)
        {
            for (int i = -3; i <= 3; i++)
            {
                var code = TotpUtils.ComputeCode(TotpSecret, i);
                authorizations.Add($"{Username}:{Password}{code}".ToBase64());
            }
        }
        else
        {
            authorizations.Add($"{Username}:{Password}".ToBase64());
        }

        return authorizations;
    }
}
