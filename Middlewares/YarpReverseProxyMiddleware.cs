using Microsoft.AspNetCore.Http.Extensions;

using Yarp.ReverseProxy.Forwarder;

namespace Brandmauer;

public class YarpReverseProxyMiddleware(
    RequestDelegate next,
    IHttpForwarder forwarder
) : ReverseProxyMiddleware
{
    class Client
    {
        public HttpMessageInvoker invoker;
        public ForwarderRequestConfig config;
    }
    static readonly Dictionary<int, Client> clientCache = new();

    protected override RequestDelegate Next => next;

    protected override async Task OnPassAsync(HttpContext context)
    {
        var feature = context.Features.Get<ReverseProxyFeature>();

        if (!feature.Route.UseYarp)
        {
            await NextAsync(context);
            return;
        }

        var path = $"/{feature.Suffix.TrimStart('/')}";

        context.Request.Path = path;

        if (!feature.Route.TryGetTimeout(out var timeout))
            timeout = 100;

        if (!clientCache.TryGetValue(timeout, out var client))
        {
            client = new();
            var _timeout = TimeSpan.FromSeconds(timeout);
            var handler = feature.Route.CreateHandler(_timeout);
            client.invoker = new(handler);
            client.config = new() { ActivityTimeout = _timeout };
            clientCache.Add(timeout, client);
        }

        var error = await forwarder.SendAsync(
            context,
            feature.Target,
            client.invoker,
            client.config,
            CustomHttpTransformer.Default
        );

        var response = context.Response;

        if (error != ForwarderError.None)
        {
            if (
                error != ForwarderError.UpgradeRequestCanceled &&
                error != ForwarderError.UpgradeResponseCanceled
            )
            {
                Console.WriteLine(
                    $"[ERROR] {error} {context.Request.Method} " +
                    $"{context.Request.GetDisplayUrl()} {path} " +
                    $"Status Code: {response.StatusCode}"
                );

                var errorFeature = context.GetForwarderErrorFeature();
                Console.WriteLine(errorFeature.Exception);
            }

            await NextAsync(context);
            return;
        }

        if (feature.Route.UseTeapot && response.HasErrorStatus())
            await NextAsync(context);
    }

    class CustomHttpTransformer : HttpTransformer
    {
        public static new readonly CustomHttpTransformer Default = new();

        public override async ValueTask TransformRequestAsync(
            HttpContext httpContext,
            HttpRequestMessage proxyRequest,
            string destinationPrefix,
            CancellationToken cancellationToken
        )
        {
            await base.TransformRequestAsync(
                httpContext,
                proxyRequest,
                destinationPrefix,
                cancellationToken
            );

            var ip = httpContext.Connection.RemoteIpAddress.ToIp();
            var scheme = httpContext.Request.Scheme;

            var headers = proxyRequest.Headers;
            headers.TryAddWithoutValidation("X-Real-IP", ip);
            headers.TryAddWithoutValidation("X-Forwarded-For", ip);
            headers.TryAddWithoutValidation("X-Forwarded-Proto", scheme);

            var feature = httpContext.Features.Get<ReverseProxyFeature>();
            headers.TryAddWithoutValidation("X-Source", feature.Source);

            switch (feature.Route.HostModification)
            {
                case ReverseProxyRoute._HostModification.Unset:
                case ReverseProxyRoute._HostModification.Origin:
                    break;

                case ReverseProxyRoute._HostModification.Target:
                    proxyRequest.RequestUri = RequestUtilities
                        .MakeDestinationAddress(
                            feature.Target,
                            httpContext.Request.Path,
                            httpContext.Request.QueryString
                        );
                    headers.Host = null;
                    break;

                case ReverseProxyRoute._HostModification.Null:
                    headers.Host = null;
                    break;
            }
        }

        public override async ValueTask<bool> TransformResponseAsync(
            HttpContext httpContext,
            HttpResponseMessage proxyResponse,
            CancellationToken cancellationToken
        )
        {
            if (proxyResponse is null)
                return false;

            if (proxyResponse.StatusCode.HasErrorStatus())
            {
                var feature = httpContext.Features.Get<ReverseProxyFeature>();
                if (feature.Route.UseTeapot)
                    return false;
            }

            httpContext.Response.Headers.TryAdd("X-Reverse-Proxy", "YARP");

            return await base.TransformResponseAsync(
                httpContext,
                proxyResponse,
                cancellationToken
            );
        }
    }
}
