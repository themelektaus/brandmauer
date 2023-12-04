namespace Brandmauer;

public class WellKnownMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<WellKnownMiddleware>(context);

        var path = context.Request.Path.ToString();

        if (path.StartsWith("/.well-known/acme-challenge/"))
        {
            var response = context.Response;
            var token = path.Split('/').LastOrDefault();

            var content = CertificateUtils.acmeChallenges.Use(
                x => x.TryGetValue(token, out var value) ? value : null
            );

            if (content is null)
            {
                response.StatusCode = 404;
            }
            else
            {
                response.ContentType = "text/plain";
                await response.Body.LoadFromAsync(content);
                goto Exit;
            }
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<WellKnownMiddleware>(context);
    }
}
