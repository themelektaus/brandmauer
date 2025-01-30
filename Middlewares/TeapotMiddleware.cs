using System.Net;

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

            var status = $"{key} {text}";

            return File.ReadAllText("wwwroot/418.html")
                .Replace("{{ Status }}", status)
                .Replace("{{ Icon }}", $"data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIiB2aWV3Qm94PSIwIDAgMjYgMjYiIGZpbGw9IiNmZmYiPjxwYXRoIGQ9Ik0xMi41IDZDMTEuNjcxODc1IDYgMTEgNi42NzE4NzUgMTEgNy41QzExIDguMzI4MTI1IDExLjY3MTg3NSA5IDEyLjUgOUMxMy4zMjgxMjUgOSAxNCA4LjMyODEyNSAxNCA3LjVDMTQgNi42NzE4NzUgMTMuMzI4MTI1IDYgMTIuNSA2IFogTSAxMi41IDkuMTg3NUMxMC4zOTQ1MzEgOS4xODc1IDguNDY0ODQ0IDEwLjMzOTg0NCA3IDEyTDE4IDEyQzE2LjUzNTE1NiAxMC4zMzk4NDQgMTQuNjA1NDY5IDkuMTg3NSAxMi41IDkuMTg3NSBaIE0gNi4yMTg3NSAxM0M1Ljk5MjE4OCAxMy4zMjQyMTkgNS43ODkwNjMgMTMuNjUyMzQ0IDUuNTkzNzUgMTRMNCAxNEMyLjI3NzM0NCAxNCAxLjA4NTkzOCAxNC42OTkyMTkgMC41MzEyNSAxNS42MjVDLTAuMDIzNDM3NSAxNi41NTA3ODEgMCAxNy41IDAgMThDMCAxOC41ODIwMzEgMC4xOTE0MDYgMTkuMDQyOTY5IDAuNSAxOS41NjI1QzAuODA4NTk0IDIwLjA4MjAzMSAxLjI2NTYyNSAyMC42MTcxODggMS44NDM3NSAyMS4xMjVDMi43MjY1NjMgMjEuODk0NTMxIDMuOTQ1MzEzIDIyLjU2MjUgNS40Njg3NSAyMi44NDM3NUM2LjExNzE4OCAyMy42ODM1OTQgNi45ODA0NjkgMjQuMzUxNTYzIDggMjQuODQzNzVMOCAyNUM4IDI1LjU1MDc4MSA4LjQ0OTIxOSAyNiA5IDI2TDE2IDI2QzE2LjU1MDc4MSAyNiAxNyAyNS41NTA3ODEgMTcgMjVMMTcgMjQuODQzNzVDMTguNDI1NzgxIDI0LjE1MjM0NCAxOS41MzkwNjMgMjMuMTA5Mzc1IDIwLjE4NzUgMjEuNzVDMjUuNDE0MDYzIDIwLjQzNzUgMjQuNDIxODc1IDEzLjkwMjM0NCAyNiAxM0wyNCAxM0MyMi43MTQ4NDQgMTMgMjMuNzkyOTY5IDE2LjExMzI4MSAyMC41MzEyNSAxNi44NDM3NUMyMC4yMDMxMjUgMTUuNTE1NjI1IDE5LjYwNTQ2OSAxNC4xNzU3ODEgMTguNzgxMjUgMTMgWiBNIDQgMTZMNC42ODc1IDE2QzQuMzU1NDY5IDE2Ljk4ODI4MSA0LjE4NzUgMTcuOTg4MjgxIDQuMTg3NSAxOC45Mzc1QzQuMTg3NSAxOS40Mjk2ODggNC4yMTg3NSAxOS45Mjk2ODggNC4zMTI1IDIwLjM3NUMzLjg3NSAyMC4xNTYyNSAzLjQ3MjY1NiAxOS45MDIzNDQgMy4xNTYyNSAxOS42MjVDMi43MzQzNzUgMTkuMjU3ODEzIDIuNDQxNDA2IDE4Ljg1NTQ2OSAyLjI1IDE4LjUzMTI1QzIuMDU4NTk0IDE4LjIwNzAzMSAyIDE3LjkxNzk2OSAyIDE4QzIgMTcuNSAyLjAyMzQzOCAxNi45NDkyMTkgMi4yMTg3NSAxNi42MjVDMi40MTQwNjMgMTYuMzAwNzgxIDIuNzIyNjU2IDE2IDQgMTZaIiBmaWxsPSIjZmZmIi8+PC9zdmc+")
                .Replace("{{ Title }}", "418 I'm a teapot")
                .Replace("{{ Text }}", "An attempt to make coffee with<br>a teapot was rejected.")
                .Replace("{{ Origin }}", $"Seriously, it's a \"{status}\" thing.");
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
