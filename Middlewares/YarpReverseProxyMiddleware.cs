using System.Collections.Concurrent;

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
    static readonly ConcurrentDictionary<int, Lazy<Client>> clientCache = new();

    protected override RequestDelegate Next => next;

    protected override string AuditType => "Proxy (YARP)";

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

        var client = clientCache.GetOrAdd(timeout, _ => new(() =>
        {
            var _timeout = TimeSpan.FromSeconds(timeout);
            var handler = feature.Route.CreateHandler(_timeout);
            return new()
            {
                invoker = new(handler),
                config = new() { ActivityTimeout = _timeout }
            };
        })).Value;

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
            var upgradeCanceled =
                error == ForwarderError.UpgradeRequestCanceled ||
                error == ForwarderError.UpgradeResponseCanceled;

            var canceled =
                error == ForwarderError.RequestCanceled ||
                context.RequestAborted.IsCancellationRequested;

            if (!upgradeCanceled && canceled)
            {
                AuditCanceled(context, error.ToString());
            }
            else if (!upgradeCanceled)
            {
                var errorFeature = context.GetForwarderErrorFeature();
                AuditError(
                    context,
                    $"Upstream request failed ({error}):",
                    errorFeature?.Exception,
                    $"Status Code: {response.StatusCode}"
                );
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

            var headers = proxyRequest.Headers;
            SetForwardedHeaders(httpContext, headers);

            var feature = httpContext.Features.Get<ReverseProxyFeature>();

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
