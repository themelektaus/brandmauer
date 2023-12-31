namespace Brandmauer;

public class LoginMiddleware(RequestDelegate next)
{
    public static readonly string SessionTokenKey
        = $"X-{Utils.Name}-Session-Token";

    public class Session
    {
        public readonly long authenticationId;
        public readonly string token;

        public Session(long authenticationId)
        {
            this.authenticationId = authenticationId;
            token = Utils.GenerateToken();
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
            request.Headers.TryGetValue(SessionTokenKey, out var authorization)
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

    [Frontend]
    static FrontendResult GetFrontend(HttpContext context)
    {
        var permission = context.Features.Get<PermissionFeature>();

        if (permission is null)
            return default;

        if (permission.Authorized)
            return default;

        if (permission.ReverseProxyRouteId != 0)
            return default;

        return new()
        {
            path = "prompt.html",
            replacements = new()
            {
                { "content", "<!--segment: login-->" }
            }
        };
    }
}
