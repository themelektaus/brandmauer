using NetTools;

namespace Brandmauer;

public class ReverseProxyPreparatorMiddleware(RequestDelegate next)
{
    public class TargetCache : ThreadsafeCache<string, string>
    {
        protected override bool Logging => true;

        protected override string GetNew(string key)
        {
            var x = key.Split("://", 2);
            var targetHost = x[1].Split(['/', ':'], 2)[0];

            if (!Utils.IsIpAddress(targetHost))
            {
                var host = Database.Use(
                    x => x.Hosts.FirstOrDefault(
                        x => x.Name == targetHost
                    )
                );
                if (host is not null && host.Addresses.Count > 0)
                {
                    var @new = host.Addresses[0].Value;
                    key = $"{x[0]}://{@new}{x[1][targetHost.Length..]}";
                }
            }

            return key;
        }
    }
    public static readonly TargetCache targetCache = new();

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<ReverseProxyPreparatorMiddleware>(context);

        var host = context.Request.Host.Host;

        if (Utils.allLocalIpAddresses.Contains(host))
            goto Next;

        if (Utils.IsIpAddress(host))
            goto Next;

        var routes = Database.Use(
            x => x.ReverseProxyRoutes.Where(x => x.Enabled).ToList()
        );

        var sources = new List<(ReverseProxyRoute route, string domain)>();

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

        (ReverseProxyRoute route, string domain) source = (null, null);

        foreach (var x in sources)
        {
            var subPath = x.domain
                .Split('/', 2)
                .Skip(1)
                .FirstOrDefault(string.Empty);

            if (!trimmedPath.StartsWith(subPath))
                continue;

            var domain = source.domain;
            if (domain is null || domain.Length < x.domain.Length)
                source = x;
        }

        if (source.route is null)
        {
            context.Response.StatusCode = 503;
            goto Next;
        }

        if (source.route.SourceHosts.Count > 0)
        {
            var ip = context.Connection.RemoteIpAddress.ToIp();
            foreach (
                var hostAddresses in source.route.SourceHosts
                    .SelectMany(x => x.Addresses)
                    .Select(x => x.Value.ToIpAddress(useCache: true)
                )
            )
            {
                foreach (var a in hostAddresses.Split(','))
                    foreach (var rangeIp in IPAddressRange.Parse(a))
                        if (rangeIp.ToIp() == ip)
                            goto Accept;
            }

            context.Response.StatusCode = 401;
            goto Next;
        }

    Accept:
        var basePath = source.domain
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
        {
            target = targetCache.Get(target);
        }
        else
        {
            if (target == string.Empty)
                goto Next;

            target = $"http://127.0.0.1:{Utils.HTTP}/{target}";
        }

        context.Features.Set(new ReverseProxyFeature()
        {
            Route = source.route,
            Domain = source.domain,
            Target = target,
            Suffix = path[(basePath.Length + 1)..]
        });

    Next:
        await next(context);

        Utils.LogOut<ReverseProxyPreparatorMiddleware>(context);
    }
}
