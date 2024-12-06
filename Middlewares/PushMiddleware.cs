namespace Brandmauer;

public class PushMiddleware(RequestDelegate next)
{
    const string PATH = "/push";

    static readonly string TokenKey = $"X-{Utils.Name}-Token";

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<PushMiddleware>(context);

        var request = context.Request;
        var path = request.Path.ToString();

        if (path.StartsWith(PATH))
            context.Features.Set(new PermissionFeature { Authorized = true });

        var headers = request.Headers;
        var response = context.Response;

        if (path == PATH && headers.TryGetValue(TokenKey, out var token))
        {
            var pushListener = Database.Use(
                x => x.PushListeners.FirstOrDefault(
                    y => y.Token == token
                )
            );

            if (pushListener is null)
            {
                response.StatusCode = 404;
                goto Exit;
            }

            Database.Use(x =>
            {
                pushListener.Touch();
                x.Save(logging: false);
            });

            goto Exit;
        }

        await next.Invoke(context);

    Exit:
        Utils.LogEnd<PushMiddleware>(context);
    }
}
