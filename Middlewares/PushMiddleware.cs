namespace Brandmauer;

public class PushMiddleware(RequestDelegate next)
{
    static readonly string TokenKey = $"X-{Utils.Name}-Token";

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<PushMiddleware>(context);

        var request = context.Request;
        var response = context.Response;

        if (
            request.Path == "/push" &&
            request.Headers.TryGetValue(TokenKey, out var token)
        )
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
                x.Save();
            });

            goto Exit;
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<PushMiddleware>(context);
    }
}
