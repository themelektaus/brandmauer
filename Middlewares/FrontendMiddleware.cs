using LibSassHost;

using Microsoft.AspNetCore.StaticFiles;

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Brandmauer;

public class FrontendMiddleware
{
    const string WWWROOT = "wwwroot";
    const string INDEX_HTML = "index.html";
    const string FAVICON_ICO = Utils.FAVICON_ICO;
    const string SEGMENTS = "segments";
    const string STATIC = Utils.STATIC;
    const string STYLE = "style";
    const string SCRIPT = "script";

    readonly RequestDelegate next;
    readonly List<MethodInfo> frontendMethods = new();

    public FrontendMiddleware(RequestDelegate next)
    {
        this.next = next;

        frontendMethods.AddRange(
            Utils.GetMethodsByAttribute<FrontendAttribute>()
        );
    }

    static string GetWwwRootFile(string path)
    {
        return Path.Combine([WWWROOT, .. path.TrimStart('/').Split('/')]);
    }

    static async Task<string> GetWwwRootFileContentAsync(string path)
    {
        path = GetWwwRootFile(path);
        return await File.ReadAllTextAsync(path);
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<FrontendMiddleware>(context);

        var response = context.Response;

        if (response.StatusCode == 200 && !response.HasStarted)
        {
            var path = context.Request.Path.ToString();

            if (Utils.IsPublicPath(path))
            {
                if (path.EndsWith(".scss"))
                {
                    static string OnLoadContent(string x)
                        => SassCompiler.Compile(x).CompiledContent;

                    await LoadAsync(context, "text/css", path, OnLoadContent);
                }
                else
                {
                    await SendFileAsync(context, path);
                }
                goto Exit;
            }

            foreach (var method in frontendMethods)
            {
                var result = (FrontendResult) method.Invoke(null, [context]);
                if (result.notFound)
                    goto NotFound;

                if (result.path is not null)
                {
                    var replacements = result.replacements ?? new();
                    await SetHtmlAsync(context, result.path, replacements);
                    goto Exit;
                }
            }

            if (path == "/")
                path = $"/{INDEX_HTML}";

            if (path == $"/{INDEX_HTML}")
            {
                await SetHtmlAsync(context, path, new());
                goto Exit;
            }

        NotFound:
            if (!path.StartsWith("/api"))
                response.StatusCode = 404;
        }

        await next.Invoke(context);

    Exit:
        Utils.LogEnd<FrontendMiddleware>(context);
    }

    static async Task LoadAsync(
        HttpContext context,
        string contentType,
        string path,
        Func<string, string> onLoadContent = null
    )
    {
        var content = await GetWwwRootFileContentAsync(path);
        if (onLoadContent is not null)
            content = onLoadContent(content);

        await SetAsync(context, contentType, content);
    }

    static async Task SetAsync(
        HttpContext context,
        string contentType,
        string content
    )
    {
        context.Response.ContentType = contentType;
        await context.Response.Body.LoadFromAsync(content);
    }

    static async Task SetHtmlAsync(
        HttpContext context,
        string path,
        Dictionary<string, object> replacements
    )
    {
        var content = await GetWwwRootFileContentAsync(path);
        for (var i = 0; i < 3; i++)
        {
            ApplySegments();
            ApplyReplacements();
        }
        ApplyStyle();
        ApplyScript();

        await SetAsync(context, "text/html", content);

        void ApplyReplacements()
        {
            content = content
                .Replace($"<!--title-->", Utils.Name)
                .Replace($"<!--host-->", context.Request.Host.Host);

            foreach (var (key, value) in replacements)
                content = content.Replace($"<!--{key}-->", value.ToString());
        }

        void ApplySegments()
        {
            var folder = Path.Combine(WWWROOT, SEGMENTS);
            var files = new DirectoryInfo(folder).EnumerateFiles();
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                var segment = File.ReadAllText(file.FullName);
                content = Regex.Replace(
                    content,
                    @$"\<\!\-\-\s*segment\:\s*{name}\s*(\{{(.*?)\}})?\s*\-\-\>",
                    m =>
                    {
                        foreach (var value in m.Groups[2].Value.Split(','))
                        {
                            var keyValue = value.Split(':');
                            if (keyValue.Length != 2)
                                continue;

                            segment = segment.Replace(
                                $"{{{keyValue[0]}}}",
                                keyValue[1]
                            );
                        }
                        return segment;
                    },
                    RegexOptions.CultureInvariant
                );
            }
        }

        void ApplyStyle()
        {
            var builder = new StringBuilder();
            var folder = Path.Combine(WWWROOT, STATIC, STYLE);
            var files = new DirectoryInfo(folder).EnumerateFiles();

            foreach (var file in files.OrderBy(x => x.Name))
            {
                var href = $"{STATIC}/{STYLE}/{file.Name}";
                builder.AppendLine(
                    $"<link rel=\"stylesheet\" href=\"{href}\">"
                );
            }

            content = content.Replace(
                $"<!--{STATIC}: {STYLE}-->",
                builder.ToString().TrimEnd()
            );
        }

        void ApplyScript()
        {
            var builder = new StringBuilder();
            var folder = Path.Combine(WWWROOT, STATIC, SCRIPT);
            var files = new DirectoryInfo(folder).EnumerateFiles();

#if DEBUG
            builder.AppendLine("<script>DEBUG = true</script>");
#else
            builder.AppendLine("<script>DEBUG = false</script>");
#endif

#if LINUX
            builder.AppendLine("<script>LINUX = true</script>");
#else
            builder.AppendLine("<script>LINUX = false</script>");
#endif

#if WINDOWS
            builder.AppendLine("<script>WINDOWS = true</script>");
#else
            builder.AppendLine("<script>WINDOWS = false</script>");
#endif

            foreach (var file in files.OrderBy(x => x.Name))
            {
                var src = $"{STATIC}/{SCRIPT}/{file.Name}";
                builder.AppendLine(
                    $"<script src=\"{src}\" defer></script>"
                );
            }

            content = content.Replace(
                $"<!--{STATIC}: {SCRIPT}-->",
                builder.ToString().TrimEnd()
            );
        }
    }

    static async Task SendFileAsync(HttpContext context, string path)
    {
        var file = GetWwwRootFile(path);

        var provider = new FileExtensionContentTypeProvider();
        if (provider.TryGetContentType(file, out var contentType))
            context.Response.ContentType = contentType;

        await context.Response.SendFileAsync(file);
    }
}
