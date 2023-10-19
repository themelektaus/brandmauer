using NetTools;

namespace Brandmauer;

public class ReverseProxyMiddleware
{
    public class TargetCache : ThreadsafeCache<string, string>
    {
        protected override string GetNew(string key)
        {
            Console.WriteLine(
                $"{nameof(TargetCache)}.GetValue() => Key: {key}"
            );

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

    public class Settings
    {
        public bool logging;

        public List<ReverseProxyRoute> routes = new();
    }

    static readonly HttpClientHandler httpClientHandler = new()
    {
        ServerCertificateCustomValidationCallback
            = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    };

    static readonly HttpClient httpClient = new(httpClientHandler);

    readonly Settings settings = new();

    readonly RequestDelegate next;
    readonly Action<Settings> updateSettings;

    public ReverseProxyMiddleware(
        RequestDelegate next,
        Action<Settings> updateSettings
    )
    {
        this.next = next;
        this.updateSettings = updateSettings;
    }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn<ReverseProxyMiddleware>(context);

        var target = string.Empty;
        var host = context.Request.Host.Host;

        if (Utils.allLocalIpAddresses.Contains(host))
            goto Break;

        if (Utils.IsIpAddress(host))
            goto Break;

        updateSettings.Invoke(settings);

        Uri url;

        var path = context.Request.Path.ToString();
        var trimmedPath = path.Trim('/');

        var sources = new List<(ReverseProxyRoute route, string domain)>();

        foreach (var route in settings.routes)
        {
            foreach (
                var sourceDomain in route.SourceDomains
                    .Where(x => x.Value.Split('/').FirstOrDefault() == host)
            )
            {
                sources.Add((route, sourceDomain.Value));
            }
        }

        (ReverseProxyRoute route, string domain) source = (null, null);

        foreach (var route in sources)
        {
            var subPath = route.domain
                .Split('/')
                .Skip(1)
                .FirstOrDefault(string.Empty);

            if (!trimmedPath.StartsWith(subPath))
                continue;

            source = route;
            if (trimmedPath == subPath)
                break;
        }

        if (source.route is not null)
        {
            if (source.route.SourceHosts.Count > 0)
            {
                var ip = context.Connection.RemoteIpAddress.ToIp();
                foreach (
                    var hostAddresses in source.route.SourceHosts
                        .SelectMany(x => x.Addresses)
                        .Select(x => x.Value.ToIpAddress()
                    )
                )
                {
                    foreach (var a in hostAddresses.Split(','))
                        foreach (var rangeIp in IPAddressRange.Parse(a))
                            if (rangeIp.ToIp() == ip)
                                goto Accept;
                }
                goto Block;
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

            target = source.route.Target.Trim('/');

            if (target.StartsWith("http://") || target.StartsWith("https://"))
            {
                target = targetCache.Get(target);
            }
            else
            {
                if (target == string.Empty)
                    goto Break;

                target = $"http://127.0.0.1:{Utils.HTTP}/{target}";
            }

            var suffix = path[(basePath.Length + 1)..];
            suffix += context.Request.QueryString;
            url = new($"{target}/{suffix.TrimStart('/')}");

            goto Pass;
        }

    Block:
        context.Response.StatusCode = 404;
        await NextAsync(context);
        return;

    Break:
        await NextAsync(context);
        return;

    Pass:
        var request = new HttpRequestMessage
        {
            Method = GetMethod(context.Request),
            RequestUri = url,
            Content = GetContent(context.Request),
        };

        foreach (var header in context.Request.Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value.ToArray()
            ))
            {
                request.Content?.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value.ToArray()
                );
            }
        }

        request.Headers.Host = url.Host;
        request.Headers.TryAddWithoutValidation(
            "X-Forwarded-For",
            context.Connection.RemoteIpAddress.ToIp()
        );

        try
        {
            var content = request.Content is null
                ? default
                : await request.Content?.ReadAsStringAsync();

            var requestInfo = CreateRequestInfo(request, content);

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted
            );

            context.Response.StatusCode = (int) response.StatusCode;

            foreach (var header in response.Headers)
                context.Response.Headers.TryAdd(
                    header.Key,
                    header.Value.ToArray()
                );

            foreach (var header in response.Content.Headers)
                context.Response.Headers.TryAdd(
                    header.Key,
                    header.Value.ToArray()
                );

            context.Response.Headers.Remove("transfer-encoding");
            context.Response.Headers.TryAdd("X-Brandmauer", Utils.Name);

            var bodyStream = response.Content.ReadAsStream();
            await context.Response.Body.LoadFromAsync(bodyStream);

            ResponseAndAddRequestInfo(requestInfo, context, bodyStream);
        }
        catch (Exception ex)
        {
            try
            {
                Console.WriteLine(ex.ToJson());
            }
            catch
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine();

            if (!path.EndsWith('/'))
            {
                context.Response.Redirect(
                    $"{path}/",
                    permanent: false,
                    preserveMethod: true
                );
                return;
            }

            context.Response.StatusCode = 404;
            await NextAsync(context);
        }
    }

    async Task NextAsync(HttpContext context)
    {
        await next(context);
        Utils.LogOut<ReverseProxyMiddleware>(context);
    }

    static HttpMethod GetMethod(HttpRequest request)
    {
        var method = request.Method;

        if (HttpMethods.IsHead(method))
            return HttpMethod.Head;

        if (HttpMethods.IsOptions(method))
            return HttpMethod.Options;

        if (HttpMethods.IsGet(method))
            return HttpMethod.Get;

        if (HttpMethods.IsPost(method))
            return HttpMethod.Post;

        if (HttpMethods.IsPut(method))
            return HttpMethod.Put;

        if (HttpMethods.IsDelete(method))
            return HttpMethod.Delete;

        if (HttpMethods.IsTrace(method))
            return HttpMethod.Trace;

        return new(method);
    }

    static StreamContent GetContent(HttpRequest request)
    {
        var method = request.Method;

        if (HttpMethods.IsGet(method))
            return default;

        if (HttpMethods.IsHead(method))
            return default;

        if (HttpMethods.IsDelete(method))
            return default;

        if (HttpMethods.IsTrace(method))
            return default;

        return new(request.Body);
    }

    RequestInfo CreateRequestInfo(HttpRequestMessage request, string content)
    {
        if (!settings.logging)
            return default;

        var requestInfo = new RequestInfo()
        {
            Method = request.Method.ToString(),
            Url = request.RequestUri.ToString(),
            Headers = request.Headers.ToString().Trim(),
        };

        if (request.Content is not null)
        {
            var contentHeaders = request.Content.Headers.ToString().Trim();
            requestInfo.ContentHeaders = contentHeaders;
            requestInfo.Content = content;
        }

        return requestInfo;
    }

    static void ResponseAndAddRequestInfo(
        RequestInfo requestInfo,
        HttpContext context,
        Stream bodyStream
    )
    {
        if (requestInfo is null)
            return;

        requestInfo.ResponseStatusCode = context.Response.StatusCode;

        requestInfo.ResponseHeaders = context.Response.Headers
            .Select(x => $"{x.Key}: {x.Value}")
            .Join(Environment.NewLine)
            .Trim();

        requestInfo.ResponseContentType = context.Response.ContentType;
        requestInfo.ResponseContent = bodyStream.ReadString();

        RequestInfo.Add(requestInfo);
    }
}
