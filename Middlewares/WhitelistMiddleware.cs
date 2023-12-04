namespace Brandmauer;

public class WhitelistMiddleware(RequestDelegate next)
{
    public class PendingRequest
    {
        public readonly string ipAddress;
        public readonly string host;
        public readonly string token;

        public PendingRequest(HttpContext context)
        {
            ipAddress = context.Connection.RemoteIpAddress.ToIp();
            host = context.Request.Host.Host;
            token = string.Empty;
            while (token.Length < 64)
                token += "abcdefghijklmnopqrstuvwxyz234567"[Random.Shared.Next(0, 32)];
        }
    }
    static readonly List<PendingRequest> pendingRequests = new();

    public static PendingRequest GetPendingRequest(string token)
    {
        return 
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<WhitelistMiddleware>(context);

        if (context.Request.Path.ToString() == "/whitelist")
        {
            PendingRequest pendingRequest;

            if (context.Request.Headers.TryGetValue("approve", out var token))
            {
                pendingRequest = pendingRequests
                    .FirstOrDefault(x => x.token == token);

                if (pendingRequest is null)
                {
                    context.Response.StatusCode = 401;
                    goto Exit;
                }

                pendingRequests.Remove(pendingRequest);

                // MyTODO: Use ID of reverse proxy route instead of host/domain...
                Database.Use(x => {
                    x.ReverseProxyRoutes.Where(y => y.SourceDomains.Contains(pendingRequest.host))
                });

                context.Response.StatusCode = 202;
                goto Exit;
            }


            pendingRequest = new(context);
            pendingRequests.Add(pendingRequest);
            context.Response.StatusCode = 201;
            await context.Response.Body.LoadFromAsync(pendingRequest.token);
            goto Exit;
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<WhitelistMiddleware>(context);
    }
}
