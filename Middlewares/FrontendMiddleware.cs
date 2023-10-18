﻿using LibSassHost;

using Microsoft.AspNetCore.StaticFiles;

using System.Text;

namespace Brandmauer;

public class FrontendMiddleware(RequestDelegate next)
{
    static readonly FileExtensionContentTypeProvider contentTypeProvider = new();

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
        return Path.Combine([GetWwwRootFolder(), .. path.TrimStart('/').Split('/')]);
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<FrontendMiddleware>(context);

        static async Task SetAsync(HttpContext context, string contentType, string content)
        {
            var response = context.Response;
            response.ContentType = contentType;
            await response.Body.LoadFromAsync(content);
        }

        var path = context.Request.Path.ToString();

        if (path.EndsWith(".scss"))
        {
            var content = await File.ReadAllTextAsync(GetWwwRootFile(path));
            content = SassCompiler.Compile(content).CompiledContent;
            await SetAsync(context, "text/css", content);
            return;
        }

        if (path == "/" || path == $"/{INDEX_HTML}")
        {
            var content = (await File.ReadAllTextAsync(Path.Combine(GetWwwRootFolder(), INDEX_HTML)))
                .Replace($"<!--{TITLE}-->", Utils.Name);

            StringBuilder builder = new();

            builder.Clear();
            foreach (var file in new DirectoryInfo(Path.Combine(GetStaticFolder(), STYLE)).EnumerateFiles().OrderBy(x => x.Name))
                builder.AppendLine($"<link rel=\"stylesheet\" href=\"{STATIC}/{STYLE}/{file.Name}\">");
            content = content.Replace($"<!--{STATIC}: {STYLE}-->", builder.ToString().TrimEnd());

            builder.Clear();
            foreach (var file in new DirectoryInfo(Path.Combine(GetStaticFolder(), SCRIPT)).EnumerateFiles().OrderBy(x => x.Name))
                builder.AppendLine($"<script src=\"{STATIC}/{SCRIPT}/{file.Name}\" defer></script>");
            content = content.Replace($"<!--{STATIC}: {SCRIPT}-->", builder.ToString().TrimEnd());

            foreach (var file in new DirectoryInfo(Path.Combine(GetWwwRootFolder(), SEGMENTS)).EnumerateFiles())
            {
                content = content.Replace(
                    $"<!--segment: {Path.GetFileNameWithoutExtension(file.Name)}-->",
                    File.ReadAllText(file.FullName)
                );
            }

            await SetAsync(context, "text/html", content);
            return;
        }

        if (path == $"/{FAVICON_ICO}" || path.StartsWith($"/{STATIC}/"))
        {
            await context.Response.SendFileAsync(GetWwwRootFile(path));
            return;
        }

        await next.Invoke(context);

        Utils.LogOut<FrontendMiddleware>(context);
    }
}
