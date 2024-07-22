using System.Text;

namespace Brandmauer;

public class Share : Model, IOnDeserialize
{
    static readonly string FILES_FOLDER = Path.Combine("Data", "Shared Files");

    public string Token { get; set; } = Utils.GenerateToken();
    public string Text { get; set; } = string.Empty;
    public List<StringValue> Files { get; set; } = new();
    public int Lifetime { get; set; }
    public string Password { get; set; } = string.Empty;

    public DateTime expiresOn;
    public TimeSpan expiresIn;

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges("flex: 2; ");
            {
                if (Name != string.Empty)
                    builder.AppendBadge("share", "name", "Name", Name);

                foreach (var file in Files)
                    builder.AppendBadge("share", "file", file.Value, null);
            }
            builder.EndBadges();

            builder.BeginBadges("flex: 1; ");
            {
                if (Lifetime > 0)
                {
                    if (expiresIn.HasExpired())
                    {
                        builder.AppendBadge(
                            "share", "expired-on", "Expired On",
                            expiresOn.ToHumanizedString()
                        );
                        builder.AppendBadge(
                            "share", "expired-for", "Expired For",
                            expiresIn.ToHumanizedString()
                        );
                    }
                    else
                    {
                        builder.AppendBadge(
                            "share", "expires-on", "Expires On",
                            expiresOn.ToHumanizedString()
                        );
                        builder.AppendBadge(
                            "share", "expires-in", "Expires In",
                            expiresIn.ToHumanizedString()
                        );
                    }
                }
                else
                {
                    builder.AppendBadge(
                        "share", "expires-never", "Expires", "Never"
                    );
                }
            }
            builder.EndBadges();

            return builder.ToString();
        }
    }

    public string GetLocalFilePath(int index)
    {
        return GetLocalFilePath(index + 1, Files[index].Value);
    }

    public string GetLocalFilePath(int number, string fileName)
    {
        Directory.CreateDirectory(FILES_FOLDER);
        return Path.Combine(
            FILES_FOLDER,
            $"{Identifier.Id:000000}-{number:000000}-{fileName}"
        );
    }

    public void OnDeserialize(Database _ = null)
    {
        expiresOn = CreationTimestamp.AddSeconds(Lifetime);
        expiresIn = expiresOn - DateTime.Now;
    }
}
