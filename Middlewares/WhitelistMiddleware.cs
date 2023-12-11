using Mjml.Net;

namespace Brandmauer;

public class WhitelistMiddleware(RequestDelegate next)
{
    const int _201_CREATED = 201;
    const int _202_ACCEPTED = 202;
    const int _208_ALREADY_REPORTED = 208;
    const int _401_UNAUTHORIZED = 401;
    const int _403_FORBIDDEN = 403;

    public class PendingRequest
    {
        public DateTime Timestamp { get; private set; }
        public long ReverseProxyRouteId { get; private set; }
        public string IpAddress { get; private set; }
        public string Token { get; private set; }

        public PendingRequest() { }

        public PendingRequest(long reverseProxyRouteId, string ipAddress)
        {
            Timestamp = DateTime.Now;
            ReverseProxyRouteId = reverseProxyRouteId;
            IpAddress = ipAddress;
            Token = string.Empty;

            var r = Random.Shared;
            while (Token.Length < 64)
                Token += "abcdefghijklmnopqrstuvwxyz234567"[r.Next(0, 32)];
        }
    }
    static readonly List<PendingRequest> pendingRequests = new();

    public static List<PendingRequest> GetPendingRequests()
    {
        return pendingRequests.ToList();
    }

    public static PendingRequest GetPendingRequest(string token)
    {
        return pendingRequests.FirstOrDefault(x => x.Token == token);
    }

    public static PendingRequest GetPendingRequest(
        long reverseProxyRouteId,
        string ipAddress
    )
    {
        return pendingRequests.FirstOrDefault(
            x => x.ReverseProxyRouteId == reverseProxyRouteId
              && x.IpAddress == ipAddress
        );
    }

    static int GetRequestStatus(
        long reverseProxyRouteId,
        string ipAddress
    )
    {
        var request = GetPendingRequest(reverseProxyRouteId, ipAddress);
        if (request is not null)
            return _401_UNAUTHORIZED;

        var route = Database.Use(
            x => x.ReverseProxyRoutes.FirstOrDefault(
                y => y.Identifier.Id == reverseProxyRouteId
            )
        );

        if (route is null)
            return _403_FORBIDDEN;

        if (!route.Whitelist.Any(x => x.Value == ipAddress))
            return _403_FORBIDDEN;

        return _202_ACCEPTED;
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<WhitelistMiddleware>(context);

        var request = context.Request;
        var path = request.Path.ToString();
        var query = request.Query;

        if (path == "/whitelist")
        {
            long reverseProxyRouteId;

            if (
                query.TryGetValue("token", out var token) &&
                query.TryGetValue("action", out var action) &&
                (action == "accept" || action == "decline")
            )
            {
                var pendingRequest = pendingRequests
                    .FirstOrDefault(x => x.Token == token);

                if (pendingRequest is null)
                {
                    context.Response.StatusCode = _401_UNAUTHORIZED;
                    goto Exit;
                }

                pendingRequests.Remove(pendingRequest);

                if (action == "accept")
                {
                    reverseProxyRouteId = pendingRequest.ReverseProxyRouteId;

                    Database.Use(x =>
                    {
                        var route = x.ReverseProxyRoutes.FirstOrDefault(
                            y => y.Identifier.Id == reverseProxyRouteId
                        );
                        route.Whitelist.Add(new(pendingRequest.IpAddress));
                    });
                }

                context.Response.StatusCode = _202_ACCEPTED;
                goto Exit;
            }

            if (
                query.TryGetValue("request", out var _request) &&
                long.TryParse(_request, out reverseProxyRouteId)
            )
            {
                var ipAddress = context.Connection.RemoteIpAddress.ToIp();

                if (
                    pendingRequests.Any(
                        x => x.IpAddress == ipAddress
                          && x.ReverseProxyRouteId == reverseProxyRouteId
                    )
                )
                {
                    context.Response.StatusCode = _208_ALREADY_REPORTED;
                }
                else
                {
                    var pendingRequest = new PendingRequest(
                        reverseProxyRouteId,
                        ipAddress
                    );

                    var reverseProxyRoute = Database.Use(
                        x => x.ReverseProxyRoutes.FirstOrDefault(
                            y => y.Identifier.Id == reverseProxyRouteId
                        )
                    );

                    if (reverseProxyRoute?.SmtpConnection is null)
                        goto SkipMail;

                    var receivers = reverseProxyRoute
                        .Receivers
                        .Select(x => x.Value)
                        .ToList();

                    if (receivers.Count == 0)
                        goto SkipMail;

                    var subject = $"🔥 Permission Request";

                    var baseUrl = Database.Use(x => x.Config.ExternalUrl);
                    if (baseUrl == string.Empty)
                        goto SkipMail;

                    var domain = reverseProxyRoute
                        .SourceDomains.FirstOrDefault().Value;

                    var requestToken = pendingRequest.Token;
                    var url = $"{baseUrl}/whitelist?token={requestToken}";

                    var mjml = File.ReadAllText("permission-request.mjml")
                        .Replace("{{ NAME }}", Utils.Name)
                        .Replace("{{ IP }}", ipAddress)
                        .Replace("{{ DOMAIN }}", domain)
                        .Replace("{{ URL }}", url);

                    var (body, errors) = new MjmlRenderer().Render(mjml);

                    if (errors.Count > 0)
                        goto SkipMail;

                    var (statusCode, text) = await reverseProxyRoute
                        .SmtpConnection
                        .SendAsync(receivers, subject, body, true);

                    if (statusCode.HasErrorStatus())
                    {
                        context.Response.StatusCode = statusCode;
                        goto Exit;
                    }

                SkipMail:
                    pendingRequests.Add(pendingRequest);
                    context.Response.StatusCode = _201_CREATED;
                }

                goto Exit;
            }

            if (
                query.TryGetValue("id", out var id) &&
                query.TryGetValue("ip", out var ip)
            )
            {
                if (!long.TryParse(id, out reverseProxyRouteId))
                {
                    context.Response.StatusCode = _401_UNAUTHORIZED;
                    goto Exit;
                }

                var status = GetRequestStatus(reverseProxyRouteId, ip);
                context.Response.StatusCode = status;
                goto Exit;
            }
        }

        await next.Invoke(context);

    Exit:
        Utils.LogOut<WhitelistMiddleware>(context);
    }
}
