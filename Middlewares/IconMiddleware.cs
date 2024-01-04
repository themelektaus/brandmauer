using Material.Icons;

using System.Text;

namespace Brandmauer;

public class IconMiddleware(RequestDelegate next)
{
    const string PATH = "/icon";

    static readonly Dictionary<string[], (string name, string color)> fileExtensionIcons = new()
    {
        { [ "jpg", "jpeg", "png", "bmp" ], ("fileimage", "#f9d") },
        { [ "tif", "tiff", "gif", "svg", "dds" ], ("fileimage", "white") },
        { [ "psd" ], ("fileimage", "#99f") },
        { [ "mp4", "mpg", "mpeg", "mov", "wmv", "avi", "mkv" ], ("localmovies", "#99f") },
        { [ "pdf", ], ("filedocument", "#f66") },
        { [ "wav", "wave", "flac", "mp3", "m4a", "aac", "wma" ], ("filemusic", "#6cf") },
        { [ "doc", "docx", "docm", "dot", "dotx" ], ("microsoftword", "#99f") },
        { [ "xls", "xlsx", "xlsm", "xls", "xlsx" ], ("microsoftexcel", "#6f6") },
        { [ "blend", ], ("blendersoftware", "white") },
        { [ "rpp", ], ("audiobook", "white") },
        { [ "txt", "log", "ini" ], ("filedocumentoutline", "white") },
        { [ "ovpn" ], ("vpn", "white") },
        { [ "zip", "rar", "7z" ], ("folderzip", "#ff6") },
        { [ "exe", "msi" ], ("application", "white") },
        { [ "deb" ], ("package", "#f90") },
        { [ "bat", "cmd" ], ("terminal", "white") },
        { [ "csv" ], ("filecsv", "#9f9") },
        { [ "crt", "cer", "pem", "pfx" ], ("certificate", "#f90") },
        { [ "key" ], ("key", "#ff6") },
        { [ "gz", "tar" ], ("compressedfile", "#f96") },
        { [ "unitypackage" ], ("unity", "white") },
        { [ "nupkg" ], ("dotnet", "white") },
        { [ "ttf", "otf", "woff", "woff2" ], ("fontsize", "#f96") },
        { [ "cs" ], ("languagecsharp", "#3f6") },
        { [ "nes" ], ("cassette", "white") },
        { [ "apk" ], ("android", "white") },
    };

    class IconCache : ThreadsafeCache<(string path, string color), string>
    {
        protected override bool Logging => false;

        protected override TimeSpan? MaxAge => default;

        protected override string GetNew(
            Dictionary<(string path, string color), TempValue> _,
            (string path, string color) key
        )
        {
            return "data:image/svg+xml;base64," +
                $"{GetSvg(key.path, key.color).ToBase64()}";
        }
    }
    static readonly IconCache iconCache = new();

    static string GetSvg(string path, string color)
    {
        var data = GetData(path);
        return "<svg xmlns=\"http://www.w3.org/2000/svg\" " +
            $"width=\"24px\" height=\"24px\" " +
            $"viewBox=\"0 0 24 24\" " +
            //$"preserveAspectRatio=\"xMidYMid meet\" " +
            $"fill=\"{color}\"><path d=\"{data}\"/></svg>";
    }

    static string GetData(string path)
    {
        path = path.Trim('/')[PATH.Length..][..^4];

        if (!Enum.TryParse<MaterialIconKind>(path, true, out var _kind))
            return null;

        return MaterialIconDataProvider.GetData(_kind);
    }

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.ToString();
        var query = request.Query;
        var features = context.Features;
        var response = context.Response;

        if (path.Trim('/').Split('/')[0] != PATH.TrimStart('/'))
            goto Next;

        features.Set(new PermissionFeature { Authorized = true });

        var fileExtension = query.TryGetValue("file-extension", out var ext)
            ? ext.ToString() : null;

        string svg;

        if (path.TrimEnd('/') == PATH && fileExtension is null)
        {
            response.ContentType = "text/html";

            var names = Enum.GetNames<MaterialIconKind>().Order();

            var builder = new StringBuilder();
            foreach (var name in names)
            {
                var url = $"icon/{name.ToLower()}.svg";
                svg = iconCache.Get((url, "white"));
                builder.Append(
                    $"<div data-url=\"{url}\" data-image=\"{svg}\"></div>"
                );
            }

            await response.Body.LoadFromAsync(
                File.ReadAllText("wwwroot/icons.html")
                    .Replace("<!--icons-->", builder.ToString())
            );
            return;
        }

        string color = null;

        if (query.TryGetValue("color", out var _color))
            color = _color;

        if (fileExtension is not null)
        {
            (string name, string color) icon = ("fileoutline", "#999");

            var _ext = ext.ToString().ToLower();

            foreach (var key in fileExtensionIcons.Keys)
            {
                if (!key.Contains(_ext))
                    continue;

                icon = fileExtensionIcons[key];
                break;
            }

            path = $"icon/{icon.name}.svg";
            color = icon.color;
        }

        if (!path.EndsWith(".svg"))
        {
            response.StatusCode = 404;
            goto Next;
        }

        color ??= "white";

        svg = GetSvg(path, color);
        if (svg is null)
        {
            response.StatusCode = 404;
            goto Next;
        }

        response.ContentType = "image/svg+xml";
        await response.Body.LoadFromAsync(svg);

        return;

    Next:
        await next.Invoke(context);
    }
}
