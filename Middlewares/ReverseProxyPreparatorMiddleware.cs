using NetTools;

using IHttpMaxRequestBodySizeFeature
    = Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature;

namespace Brandmauer;

using Usage = ReverseProxyRoute._WhitelistUsage;

public class ReverseProxyPreparatorMiddleware(RequestDelegate next)
{
    public class TargetCache : ThreadsafeCache<string, string>
    {
        protected override bool Logging => true;

        protected override TimeSpan? MaxAge => default;

        protected override string GetNew(string key)
        {
            var x = key.Split("://", 2);
            var targetHost = x[1].Split(['/', ':'], 2)[0];
            var targetIpAddress = targetHost.ToIpAddress(justLocal: true);
            targetIpAddress ??= targetHost;
            return $"{x[0]}://{targetIpAddress}{x[1][targetHost.Length..]}";
        }
    }
    public static readonly TargetCache targetCache = new();

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin<ReverseProxyPreparatorMiddleware>(context);

        var host = context.Request.Host.Host;

        if (Utils.allLocalIpAddresses.Contains(host))
            goto Next;

        if (Utils.IsIpAddress(host))
            goto Next;

        var routes = Database.Use(
            x => x.ReverseProxyRoutes.Where(x => x.Enabled).ToList()
        );

        var sources = new List<(ReverseProxyRoute route, string value)>();

        foreach (var route in routes)
        {
            foreach (
                var sourceDomain in route.SourceDomains
                    .Where(x => x.Value.Split('/').FirstOrDefault() == host)
            )
            {
                sources.Add((route, sourceDomain.Value));
            }
        }

        var path = context.Request.Path.ToString();
        var trimmedPath = path.Trim('/');

        (ReverseProxyRoute route, string value) source = (null, null);

        foreach (var x in sources)
        {
            var subPath = x.value
                .Split('/', 2)
                .Skip(1)
                .FirstOrDefault(string.Empty);

            if (!trimmedPath.StartsWith(subPath))
                continue;

            var value = source.value;
            if (value is null || value.Length < x.value.Length)
                source = x;
        }

        if (source.route is null)
        {
            context.Response.StatusCode = 503;
            goto Next;
        }

        var authorized = false;
        var unauthorized = false;

        var permission = context.Features.Get<PermissionFeature>();
        if (permission is not null)
        {
            authorized = permission.Authorized;
            unauthorized = !authorized;
        }

        var checkUnknownSourceHosts = true;

        if (path == "/api/time")
        {
            authorized = true;
            unauthorized = false;
            goto Accept;
        }

        if (authorized)
        {
            goto Accept;
        }

        if (source.route.SourceHosts.Count > 0)
        {
            var ip = context.Connection.RemoteIpAddress.ToIp();

            var hostAddressesBundle = source.route.SourceHosts
                .SelectMany(x => x.Addresses)
                .SelectMany(x => x.Value.ToIpAddresses())
                .ToList();

            if (source.route.WhitelistUsage == Usage.Deactivated)
            {
                if (hostAddressesBundle.Count == 0)
                    goto Accept;
            }

            foreach (var hostAddresses in hostAddressesBundle)
            {
                foreach (var a in hostAddresses.Split(','))
                {
                    foreach (var rangeIp in IPAddressRange.Parse(a))
                    {
                        if (rangeIp.ToIp() == ip)
                        {
                            checkUnknownSourceHosts = false;
                            goto Accept;
                        }
                    }
                }
            }

            if (Utils.IsPublicPath(path))
            {
                goto Accept;
            }

            if (source.route.WhitelistUsage == Usage.Deactivated)
            {
                context.Response.StatusCode = 401;
                goto Next;
            }
        }

    Accept:
        var basePath = source.value
            .Split('/')
            .Skip(1)
            .FirstOrDefault(string.Empty);

        if (basePath != string.Empty && basePath == path.TrimStart('/'))
        {
            context.Response.Redirect(
                $"../{basePath}/",
                permanent: false,
                preserveMethod: true
            );
            return;
        }

        var target = source.route.Target.Trim('/');

        if (target.StartsWith("http://") || target.StartsWith("https://"))
            target = targetCache.Get(target);

        else if (target != string.Empty)
            target = $"http://127.0.0.1:{Utils.HTTP}/{target}";

        if (!authorized)
        {
            var authentications = source.route.Authentications;
            if (authentications.Count > 0)
            {
                var cookies = context.Request.Cookies;
                var key = LoginMiddleware.SessionTokenKey;
                if (cookies.TryGetValue(key, out var sessionToken))
                {
                    if (
                        authentications.Any(
                            x => LoginMiddleware.IsAuthorized(x, sessionToken)
                        )
                    )
                    {
                        goto Authorized;
                    }
                }

                context.Features.Set(new PermissionFeature
                {
                    Authorized = false
                });
                goto Next;
            }
        }

        var maxBodySize = source.route.MaxBodySize.GetBytes();

        if (maxBodySize is not null)
        {
            context.Features.Get<IHttpMaxRequestBodySizeFeature>()
                .MaxRequestBodySize = maxBodySize;
        }

    Authorized:
        if (!authorized)
        {
            var usage = source.route.WhitelistUsage;

            if (usage != Usage.Deactivated)
            {
                var isForced = usage == Usage.Forced;
                var allowSourceHosts = usage == Usage.AllowSourceHosts;

                if (isForced || (checkUnknownSourceHosts && allowSourceHosts))
                {
                    var ip = context.Connection.RemoteIpAddress.ToIp();

                    if (
                        !source.route.Whitelist.Any(
                            x => x.Value == ip && !x.ExpiresIn.HasExpired()
                        )
                    )
                    {
                        context.Features.Set(new PermissionFeature
                        {
                            Authorized = false,
                            ReverseProxyRouteId = source.route.Identifier.Id
                        });
                        goto Next;
                    }
                }
            }
        }

        var useScript = source.route.Script != string.Empty;
        if (!unauthorized && (target != string.Empty || useScript))
        {
            context.Features.Set(new ReverseProxyFeature
            {
                Route = source.route,
                Source = source.value,
                Target = target,
                Suffix = path[(basePath.Length + 1)..],
                UseScript = useScript
            });
        }

    Next:
        await next(context);

        Utils.LogEnd<ReverseProxyPreparatorMiddleware>(context);
    }
}
