namespace Brandmauer;

public class TeapotMiddleware
{
    public class Configuration(string htmlFile, Dictionary<int, string> status)
    {
        public void Spill(int code, string message)
        {
            status.Add(
                code,
                File.ReadAllText(htmlFile)
                    .Replace("{{ Status }}", $"{code} {message}")
            );
        }
    }

    readonly RequestDelegate next;
    readonly int[] statusCodeBreakers = [];
    readonly Dictionary<int, string> status = new();

    public TeapotMiddleware(RequestDelegate next, string htmlFile, int[] statusCodeBreakers, Action<Configuration> setup)
    {
        this.next = next;
        this.statusCodeBreakers = statusCodeBreakers;

        setup(new(htmlFile, status));
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<TeapotMiddleware>(context);

        var response = context.Response;

        if (Array.IndexOf(statusCodeBreakers, response.StatusCode) == -1)
            await next.Invoke(context);

        foreach (var code in status.Keys)
        {
            if (response.StatusCode != code)
                continue;

            response.StatusCode = StatusCodes.Status418ImATeapot;
            response.ContentType = "text/html";

            await response.Body.LoadFromAsync(status[code]);
            break;
        }

        Utils.LogOut<TeapotMiddleware>(context);
    }
}
