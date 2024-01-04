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

    public static string FromBytes(this byte[] @this)
    {
        return Encoding.UTF8.GetString(@this);
    }

    public static MemoryStream ToStream(this string @this)
    {
        return @this.ToBytes().ToStream();
    }

    public static MemoryStream ToStream(this byte[] @this)
    {
        return new MemoryStream(@this);
    }

    public static string ToBase64(this string @this)
    {
        return @this.ToBytes().ToBase64();
    }

    public static string ToBase64(this byte[] @this)
    {
        return Convert.ToBase64String(@this);
    }

    public static string FromBase64(this string @this)
    {
        return Convert.FromBase64String(@this).FromBytes();
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

    class DnsCache : ThreadsafeCache<string, string[]>
    {
        protected override bool Logging => true;

        protected override TimeSpan? MaxAge => TimeSpan.FromHours(1);

        int currentDepth;
        readonly Dictionary<string, string[]> pending = new();

        public bool justLocal;

        protected override string[] GetNew(
            Dictionary<string, TempValue> x,
            string key
        )
        {
            if (key == string.Empty)
                return [];

            if (Utils.IsIpAddress(key))
                return [key];

            List<string> addresses = [];

            if (!justLocal)
            {
                try
                {
                    var hostEntry = Dns.GetHostEntry(key);

                    var ipAddress = hostEntry.AddressList
                        .FirstOrDefault(
                            x => x.AddressFamily == AddressFamily.InterNetwork
                        );

                    if (ipAddress is not null)
                        addresses.Add(ipAddress.ToString());
                }
                catch
                {

                }
            }

            var host = Database.Use(
                x => x.Hosts.FirstOrDefault(
                    x => x.Name == key
                )
            );

            if (host is not null)
            {
                if (currentDepth++ < 5)
                {
                    var hostAddresses = host.Addresses
                        .Select(x => x.Value)
                        .ToList();

                    pending.Add(key, [.. addresses]);

                    foreach (var hostAddress in hostAddresses)
                        if (!pending.ContainsKey(hostAddress))
                            addresses.AddRange(GetUnsafe(x, hostAddress));

                    pending.Remove(key);
                }
                currentDepth--;
            }

            var result = addresses.Distinct().ToArray();
            return result.Length == 0 ? null : result;
        }
    }
    static readonly DnsCache dnsCache = new();

    public static string ToIpAddress(this string @this, bool justLocal = false)
    {
        return @this.ToIpAddresses(justLocal)?.FirstOrDefault();
    }

    public static string[] ToIpAddresses(
        this string @this,
        bool justLocal = false
    )
    {
        dnsCache.justLocal = justLocal;
        return dnsCache.Get(@this);
    }

    public static string ToIp(this IPAddress @this)
    {
        var ip = @this.MapToIPv4().ToString();

        if (ip == "0.0.0.1")
            return "127.0.0.1";

        return ip;
    }

    public static StringBuilder BeginBadgesGroup(this StringBuilder @this)
        => @this.Append("<div class=\"badges-group\">");

    public static StringBuilder EndBadgesGroup(this StringBuilder @this)
        => @this.Append("</div>");

    public static StringBuilder BeginBadgesEnd(this StringBuilder @this)
        => @this.Append("<div class=\"badges-end\">");

    public static StringBuilder EndBadgesEnd(this StringBuilder @this)
        => @this.Append("</div>");

    public static StringBuilder BeginBadges(this StringBuilder @this, string style = null)
        => @this.Append($"<div class=\"badges\"{(style is null ? "" : $" style=\"{style}\"")}>");

    public static StringBuilder EndBadges(this StringBuilder @this)
        => @this.Append("</div>");

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

    public static bool HasErrorStatus(this HttpResponse @this)
    {
        return ((HttpStatusCode) @this.StatusCode).HasErrorStatus();
    }

    public static bool HasErrorStatus(this HttpStatusCode @this)
    {
        return ((int) @this).HasErrorStatus();
    }

    public static bool HasErrorStatus(this int @this)
    {
        return @this - 400 >= 0;
    }

    public static IEnumerable<T> TakeLast<T>(
        this IEnumerable<T> @this,
        int count
    ) => @this.Skip(Math.Max(0, @this.Count() - count));

    public static string ToHumanizedSize(this long @this)
    {
        float x = @this;
        var u = new[] { "B", "KiB", "MiB", "GiB", "TiB" };
        var i = 0;
        while (x >= 1024 && i++ < u.Length - 1) x /= 1024;
        return $"{x:0.00} {u[i]}";
    }
}
