namespace Brandmauer;

public class LoginMiddleware(RequestDelegate next)
{
    public static readonly string SessionTokenKey = $"X-{Utils.Name}-Session-Token";

    public class Session
    {
        public readonly long authenticationId;
        public readonly string token;

        public Session(long authenticationId)
        {
            this.authenticationId = authenticationId;
            token = string.Empty;
            while (token.Length < 64)
                token += "abcdefghijklmnopqrstuvwxyz234567"[Random.Shared.Next(0, 32)];
        }
    }
    static readonly List<Session> sessions = new();

    public static bool IsAuthorized(
        Authentication authentication,
        string sessionToken
    )
    {
        return sessions.Any(x
            => x.authenticationId == authentication.Identifier.Id
            && x.token == sessionToken
        );
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<LoginMiddleware>(context);

        var request = context.Request;
        var path = request.Path.ToString();

        if (
            path == "/login" &&
            request.Form.TryGetValue(SessionTokenKey, out var authorization)
        )
        {
            var authentication = Database.Use(
                x => x.Authentications.FirstOrDefault(
                    y => y.GetAuthorizations().Contains(authorization)
                )
            );

            if (authentication is not null)
            {
                var newSession = new Session(authentication.Identifier.Id);
                sessions.Add(newSession);

                context.Response.Cookies.Append(
                    SessionTokenKey,
                    newSession.token,
                    new()
                    {
                        Domain = request.Host.Host,
                        Path = "/"
                    }
                );
                goto Exit;
            }
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<LoginMiddleware>(context);
    }
}
