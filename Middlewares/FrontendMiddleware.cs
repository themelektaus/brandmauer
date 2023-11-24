using LibSassHost;

using Microsoft.AspNetCore.StaticFiles;

using System.Text;

namespace Brandmauer;

public class FrontendMiddleware(RequestDelegate next)   
{
    const string WWWROOT = "wwwroot"/*-htmx*/;
    const string INDEX_HTML = "index.html"/*.htmx*/;
    const string FAVICON_ICO = "favicon.ico";
    const string TITLE = "title";
    const string SEGMENTS = "segments";
    const string STATIC = "static";
    const string STYLE = "style";
    const string SCRIPT = "script";

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
                return;
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
                await IndexHtml(context, path);
                return;
            }

            if (path == $"/{FAVICON_ICO}" || path.StartsWith($"/{STATIC}/"))
            {
                await SendFileAsync(context, path);
                return;
            }

            if (!path.StartsWith("/api"))
                context.Response.StatusCode = 404;
        }

        await next.Invoke(context);

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

    static async Task IndexHtml(HttpContext context, string path)
    {
        var content = await GetWwwRootFileContentAsync(path);
        content = content.Replace($"<!--{TITLE}-->", Utils.Name);

        StringBuilder builder = new();
        string folder;
        IEnumerable<FileInfo> files;

        builder.Clear();
        folder = Path.Combine(WWWROOT, STATIC, STYLE);
        files = new DirectoryInfo(folder).EnumerateFiles();

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

        builder.Clear();
        folder = Path.Combine(WWWROOT, STATIC, SCRIPT);
        files = new DirectoryInfo(folder).EnumerateFiles();

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

        folder = Path.Combine(WWWROOT, SEGMENTS);
        files = new DirectoryInfo(folder).EnumerateFiles();
        foreach (var file in files)
        {
            var name = Path.GetFileNameWithoutExtension(file.Name);
            content = content.Replace(
                $"<!--segment: {name}-->",
                File.ReadAllText(file.FullName)
            );
        }

        await SetAsync(context, "text/html", content);
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
