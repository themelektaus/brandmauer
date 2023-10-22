using LibSassHost;

using System.Text;

namespace Brandmauer;

public class FrontendMiddleware(RequestDelegate next)
{
    const string WWWROOT = "wwwroot";
    const string INDEX_HTML = "index.html";
    const string FAVICON_ICO = "favicon.ico";
    const string TITLE = "title";
    const string SEGMENTS = "segments";
    const string STATIC = "static";
    const string STYLE = "style";
    const string SCRIPT = "script";

    static string GetWwwRootFolder()
    {
        return WWWROOT;
    }

    static string GetStaticFolder()
    {
        return Path.Combine(GetWwwRootFolder(), STATIC);
    }

    static string GetWwwRootFile(string path)
    {
        return Path.Combine([
             GetWwwRootFolder(),
            .. path.TrimStart('/').Split('/')
         ]);
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<FrontendMiddleware>(context);

        if (context.Response.StatusCode == 200 && !context.Response.HasStarted)
        {
            var path = context.Request.Path.ToString();

            if (path.EndsWith(".scss"))
            {
                await Css(context, path);
                return;
            }

            if (path == "/")
                path = $"/{INDEX_HTML}";

            if (path == $"/{INDEX_HTML}")
            {
                await IndexHtml(context, path);
                return;
            }

            if (path == $"/{FAVICON_ICO}" || path.StartsWith($"/{STATIC}/"))
            {
                await FavIcon(context, path);
                return;
            }
        }

        await next.Invoke(context);

        Utils.LogOut<FrontendMiddleware>(context);
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

    async Task Css(HttpContext context, string path)
    {
        var content = await File.ReadAllTextAsync(GetWwwRootFile(path));
        content = SassCompiler.Compile(content).CompiledContent;
        await SetAsync(context, "text/css", content);
    }

    async Task IndexHtml(HttpContext context, string path)
    {
        var content = await File.ReadAllTextAsync(GetWwwRootFile(path));
        content = content.Replace($"<!--{TITLE}-->", Utils.Name);

        StringBuilder builder = new();
        string folder;
        IEnumerable<FileInfo> files;

        builder.Clear();
        folder = Path.Combine(GetStaticFolder(), STYLE);
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
        folder = Path.Combine(GetStaticFolder(), SCRIPT);
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

        folder = Path.Combine(GetWwwRootFolder(), SEGMENTS);
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

    async Task FavIcon(HttpContext context, string path)
    {
        await context.Response.SendFileAsync(GetWwwRootFile(path));
    }
}
