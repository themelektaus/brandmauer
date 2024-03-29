﻿using System.Net;

namespace Brandmauer;

public class TeapotMiddleware(RequestDelegate next)
{
    class HtmlFileCache : ThreadsafeCache<int, string>
    {
        protected override bool Logging => false;

        protected override TimeSpan? MaxAge => default;

        protected override string GetNew(int key)
        {
            var statusCode = (HttpStatusCode) key;
            var statusCodeString = key == 418
                ? "Unknown"
                : statusCode.ToString();

            var text = Utils.CamelSpaceRegex()
                .Replace(statusCodeString, " $1");

            return File.ReadAllText("wwwroot/418.html")
                .Replace("{{ Status }}", $"{key} {text}");
        }
    }
    static readonly HtmlFileCache htmlFileCache = new();

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<TeapotMiddleware>(context);

        var response = context.Response;

        if (response.HasErrorStatus())
        {
            var content = htmlFileCache.Get(response.StatusCode);
            response.StatusCode = StatusCodes.Status418ImATeapot;
            response.ContentType = "text/html";
            await response.Body.LoadFromAsync(content);
            return;
        }

        await next.Invoke(context);

        Utils.LogEnd<TeapotMiddleware>(context);
    }
}
