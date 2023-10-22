﻿using System.Diagnostics;
using System.Net;

using Yarp.ReverseProxy.Forwarder;

namespace Brandmauer;

public class YarpReverseProxyMiddleware(
    RequestDelegate next,
    IHttpForwarder forwarder
) : ReverseProxyMiddleware
{
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

        var error = await forwarder.SendAsync(
            context,
            feature.Target,
            httpClient,
            ForwarderRequestConfig.Empty,
            CustomHttpTransformer.Default
        );

        var response = context.Response;

        if (error != ForwarderError.None)
        {
            var errorFeature = context.GetForwarderErrorFeature();
            Console.WriteLine(errorFeature.Exception);

            response.StatusCode = 500;
            await NextAsync(context);

            return;
        }

        if (response.HasErrorStatus())
            await NextAsync(context);
    }

    readonly HttpMessageInvoker httpClient = new(new SocketsHttpHandler()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ActivityHeadersPropagator = new ReverseProxyPropagator(
            DistributedContextPropagator.Current
        ),
        ConnectTimeout = TimeSpan.FromSeconds(15),
        SslOptions = {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        }
    });

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

            proxyRequest.Headers.TryAddWithoutValidation("X-Real-IP", ip);
            proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
            proxyRequest.Headers.TryAddWithoutValidation("X-Forwarded-Proto", scheme);

            var feature = httpContext.Features.Get<ReverseProxyFeature>();

            if (!feature.Route.KeepHost)
            {
                proxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(
                    feature.Target,
                    httpContext.Request.Path,
                    httpContext.Request.QueryString
                );
                proxyRequest.Headers.Host = null;
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
                return false;

            httpContext.Response.Headers.TryAdd("X-Reverse-Proxy", "YARP");

            return await base.TransformResponseAsync(
                httpContext,
                proxyResponse,
                cancellationToken
            );
        }
    }
}
