#if DEBUG
namespace Brandmauer;

public class HelloWorldMiddleware(RequestDelegate next)
{
    const string PATH = "/hello-world";

    public async Task Invoke(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.ToString();
        
        if (path.StartsWith(PATH))
        {
            context.Features.Set(new PermissionFeature { Authorized = true });
            var response = context.Response;
            response.ContentType = "text/html";
            await response.Body.LoadFromAsync(@"<!doctype html>
<html>
    <head>
        <style>
            body {
                color: white;
                background: linear-gradient(#001, #223);
                font-family: sans-serif;
                font-size: 2em;
                display: flex;
                justify-content: center;
                align-items: center;
                height: 100vh;
                margin: 0;
                letter-spacing: .125rem;
            }
        </style>
    </head>
    <body>Hello, World!</body>
</html>");
            return;
        }

        await next.Invoke(context);
    }
}
#endif
