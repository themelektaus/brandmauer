namespace Brandmauer;

public class PushMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<PushMiddleware>(context);

        var request = context.Request;
        var response = context.Response;

        if (request.Path == "/push")
        {
            if (!request.Query.TryGetValue("token", out var token))
            {
                response.StatusCode = 401;
                goto Exit;
            }

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
