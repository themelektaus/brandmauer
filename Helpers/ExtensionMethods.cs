using System.Net;
using System.Net.Sockets;

using System.Security.Cryptography.X509Certificates;

using System.Text;
using System.Text.Json;

namespace Brandmauer;

public static class ExtensionMethods
{
    public static string ReadString(this Stream @this)
    {
        string result;
        using (var reader = new StreamReader(@this, Encoding.UTF8))
            result = reader.ReadToEnd();
        return result;
    }

    public static byte[] ToBytes(this string @this)
    {
        return Encoding.UTF8.GetBytes(@this);
    }

    public static MemoryStream ToStream(this string @this)
    {
        return @this.ToBytes().ToStream();
    }

    public static MemoryStream ToStream(this byte[] @this)
    {
        return new MemoryStream(@this);
    }

    public static void AddIfNotNull(this IList<string> @this, string item)
    {
        @this.AddIfNotNull("", item);
    }

    public static void AddIfNotNull(
        this IList<string> @this,
        string prefix,
        string item,
        string suffix = ""
    )
    {
        if (item is null)
            return;

        @this.Add($"{prefix}{item}{suffix}");
    }

    static JsonSerializerOptions jsonOptions;

    public static string ToJson<T>(this T @this)
    {
        if (jsonOptions is null)
        {
            jsonOptions = new()
            {
                WriteIndented = true,
                IgnoreReadOnlyProperties = true,
            };
            jsonOptions.Converters.Add(new ExceptionConverter());
        }
        return JsonSerializer.Serialize(@this, jsonOptions);
    }

    public static T FromJson<T>(this string @this)
    {
        return JsonSerializer.Deserialize<T>(@this);
    }

    public static string Join<T>(this IEnumerable<T> @this)
    {
        return @this.Join(string.Empty);
    }

    public static string Join<T>(this IEnumerable<T> @this, char separator)
    {
        return string.Join(separator, @this);
    }

    public static string Join<T>(this IEnumerable<T> @this, string separator)
    {
        return string.Join(separator, @this);
    }

    public static string JoinWrap<T>(
        this IEnumerable<T> @this,
        string prefix,
        string suffix
    )
    {
        return $"{prefix}{@this.Join($"{suffix}{prefix}")}{suffix}";
    }

    public static string ToIpAddress(this string @this)
    {
        if (@this == string.Empty)
            goto Return;

        if (Utils.IsIpAddress(@this))
            goto Return;

        try
        {
            var hostEntry = Dns.GetHostEntry(@this);

            var ipAddress = hostEntry.AddressList
                .OrderByDescending(
                    x => x.AddressFamily == AddressFamily.InterNetwork
                )
                .FirstOrDefault();

            if (ipAddress is not null)
                @this = ipAddress.ToString();
        }
        catch
        {

        }

    Return:
        return @this;
    }

    public static string ToIp(this IPAddress @this)
    {
        var ip = @this.MapToIPv4().ToString();

        if (ip == "0.0.0.1")
            return "127.0.0.1";

        return ip;
    }

    public static StringBuilder AppendBadge(
        this StringBuilder @this,
        string type,
        string property,
        object text,
        object suffix
    )
    {
        var classes = $"badge badge-{type} badge-{type}-{property}";
        @this.Append($"<span class=\"{classes}\">");
        @this.Append(text);
        if (suffix is not null)
        {
            @this.Append("<span>");
            @this.Append(suffix);
            @this.Append("</span>");
        }
        @this.Append("</span>");

        return @this;
    }

    public static string ToHumanReadableDaysString(this TimeSpan @this)
    {
        var days = (int) @this.TotalDays;

        if (days < 1)
            return "today";

        if (days == 1)
            return "1 day";

        return $"{days} days";
    }

    public static Dictionary<string, string> ToDictonary(
        this X500DistinguishedName @this
    )
    {
        var keyValues = new Dictionary<string, string>();
        foreach (var part in @this.Name.Split(','))
        {
            var keyValue = part.Trim().Split('=');
            if (keyValue.Length != 2)
                continue;

            keyValues.Add(keyValue[0], keyValue[1]);
        }
        return keyValues;
    }

    public static async Task LoadFromAsync(this Stream @this, string content)
    {
        using var stream = content.ToStream();
        await @this.LoadFromAsync(stream);
    }

    public static async Task LoadFromAsync(this Stream @this, byte[] content)
    {
        using var stream = content.ToStream();
        await @this.LoadFromAsync(stream);
    }

    public static async Task LoadFromAsync(this Stream @this, Stream stream)
    {
        await stream.CopyToAsync(@this);
    }
}
