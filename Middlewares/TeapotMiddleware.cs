using System.Net;

namespace Brandmauer;

public class TeapotMiddleware
{
    readonly RequestDelegate next;

    class HtmlFileCache : ThreadsafeCache<int, string>
    {
        protected override string GetNew(int key)
        {
            var statusCode = (HttpStatusCode) key;

            var text = Utils.CamelSpaceRegex()
                .Replace(statusCode.ToString(), " $1");

            return File.ReadAllText("wwwroot/418.html")
                .Replace("{{ Status }}", $"{key} {text}");
        }
    }
    static readonly HtmlFileCache htmlFileCache = new();

    public TeapotMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<TeapotMiddleware>(context);

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

        Utils.LogOut<TeapotMiddleware>(context);
    }
}
