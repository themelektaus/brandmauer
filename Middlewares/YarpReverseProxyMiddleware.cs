using System.Diagnostics;
using System.Net;

using Yarp.ReverseProxy.Forwarder;

namespace Brandmauer;

public class YarpReverseProxyMiddleware : ReverseProxyMiddleware
{
    protected override RequestDelegate Next { get; init; }

    readonly IHttpForwarder forwarder;

    public YarpReverseProxyMiddleware(
        RequestDelegate next,
        IHttpForwarder forwarder
    )
    {
        Next = next;
        this.forwarder = forwarder;
    }

    protected override async Task OnPassAsync(HttpContext context)
    {
        if (!Feature.Route.UseYarp)
        {
            await NextAsync(context);
            return;
        }

        context.Response.Headers.TryAdd("X-Reverse-Proxy", "YARP");

        var path = $"/{Feature.Suffix.TrimStart('/')}";
        context.Request.Path = path;

        var error = await forwarder.SendAsync(
            context,
            Feature.Target,
            httpClient,
            ForwarderRequestConfig.Empty,
            CustomHttpTransformer.Default
        );

        var response = context.Response;

        if (response.StatusCode == 301 || error != ForwarderError.None)
        {
            if (!path.EndsWith('/'))
            {
                response.Redirect(
                    $"{path}/",
                    permanent: false,
                    preserveMethod: true
                );
                return;
            }

            var errorFeature = context.GetForwarderErrorFeature();
            Console.WriteLine(errorFeature.Exception);

            response.StatusCode = 500;
            await NextAsync(context);

            return;
        }

        if (response.HasErrorStatus())
            await NextAsync(context);
    }

    readonly HttpMessageInvoker httpClient = new(
        new SocketsHttpHandler()
        {
            UseProxy = false,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.None,
            UseCookies = false,
            ActivityHeadersPropagator = new ReverseProxyPropagator(
                DistributedContextPropagator.Current
            ),
            ConnectTimeout = TimeSpan.FromSeconds(15),
            SslOptions = {
                RemoteCertificateValidationCallback = (_, _, _, _) => true
            }
        }
    );

    class CustomHttpTransformer : HttpTransformer
    {
        public static new readonly CustomHttpTransformer Default = new();

        public override async ValueTask<bool> TransformResponseAsync(
            HttpContext httpContext,
            HttpResponseMessage proxyResponse,
            CancellationToken cancellationToken
        )
        {
            if (proxyResponse is null)
                return false;

            if (proxyResponse.StatusCode.HasErrorStatus())
                return false;

            return await base.TransformResponseAsync(
                httpContext,
                proxyResponse,
                cancellationToken
            );
        }

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
            proxyRequest.Headers.Add("X-Forwarded-For", ip);
            proxyRequest.Headers.Host = null;
        }
    }
}
