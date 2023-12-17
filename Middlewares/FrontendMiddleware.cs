using LibSassHost;

using Microsoft.AspNetCore.StaticFiles;

using System.Reflection;
using System.Text;

using F = System.Reflection.BindingFlags;

namespace Brandmauer;

public class FrontendMiddleware
{
    const string WWWROOT = "wwwroot"/*-htmx*/;
    const string INDEX_HTML = "index.html"/*.htmx*/;
    const string FAVICON_ICO = "favicon.ico";
    const string SEGMENTS = "segments";
    const string STATIC = "static";
    const string STYLE = "style";
    const string SCRIPT = "script";

    readonly RequestDelegate next;
    readonly List<MethodInfo> frontendMethods = new();

    public FrontendMiddleware(RequestDelegate next)
    {
        this.next = next;

        var types = GetType().Assembly.GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(F.Public | F.NonPublic | F.Static);
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<FrontendAttribute>();
                if (attribute is not null)
                    frontendMethods.Add(method);
            }
        }
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
        Utils.LogIn<FrontendMiddleware>(context);

        if (context.Response.StatusCode == 200 && !context.Response.HasStarted)
        {
            var path = context.Request.Path.ToString();

            if (path.EndsWith(".scss"))
            {
                static string OnLoadContent(string x)
                    => SassCompiler.Compile(x).CompiledContent;

                await LoadAsync(context, "text/css", path, OnLoadContent);
                goto Exit;
            }

            if (path == $"/{FAVICON_ICO}" || path.StartsWith($"/{STATIC}/"))
            {
                await SendFileAsync(context, path);
                goto Exit;
            }

            foreach (var method in frontendMethods)
            {
                var result = (FrontendResult) method.Invoke(null, [context]);
                if (result.notFound)
                    goto NotFound;

                if (result.path is not null)
                {
                    await Html(context, result.path, result.replacements ?? new());
                    goto Exit;
                }
            }

            if (path == "/")
                path = $"/{INDEX_HTML}";

            //if (path.EndsWith(".htmx"))
            //{
            //    await LoadAsync(context, "text/html", path);
            //    return;
            //}

            if (path == $"/{INDEX_HTML}")
            {
                await Html(context, path, new());
                goto Exit;
            }

        NotFound:
            if (!path.StartsWith("/api"))
                context.Response.StatusCode = 404;
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<FrontendMiddleware>(context);
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
        var response = context.Response;
        response.ContentType = contentType;
        await response.Body.LoadFromAsync(content);
    }

    static async Task Html(
        HttpContext context,
        string path,
        Dictionary<string, object> replacements
    )
    {
        var content = await GetWwwRootFileContentAsync(path);

        ApplyReplacements();
        ApplySegments();
        ApplyReplacements();
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
                content = content.Replace(
                    $"<!--segment: {name}-->",
                    File.ReadAllText(file.FullName)
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
