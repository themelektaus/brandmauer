using System.Diagnostics;
using System.Net;

using Yarp.ReverseProxy.Forwarder;

namespace Brandmauer;

public abstract class ReverseProxyMiddleware
{
    protected static readonly SocketsHttpHandler handler = new()
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
    };

    protected abstract RequestDelegate Next { get; }

    protected abstract Task OnPassAsync(HttpContext context);

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin(GetType(), context);

        var feature = context.Features.Get<ReverseProxyFeature>();
        if (feature is null || feature.Target is null)
        {
            await NextAsync(context);
            return;
        }

        await OnPassAsync(context);
    }

    protected async Task NextAsync(HttpContext context)
    {
        await Next(context);

        Utils.LogEnd(GetType(), context);
    }
}
