using System.Text;

namespace Brandmauer;

public class Share : Model
{
    static readonly string FILES_FOLDER = Path.Combine("Data", "Shared Files");

    public string Token { get; set; } = Utils.GenerateToken();
    public string Text { get; set; } = string.Empty;
    public List<StringValue> Files { get; set; } = new();
    public string Password { get; set; } = string.Empty;

    public override string HtmlInfo
    {
        get
        {
            var builder = new StringBuilder();

            builder.BeginBadges();
            {
                foreach (var file in Files)
                    builder.AppendBadge("share", "file", file.Value, null);
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
}
