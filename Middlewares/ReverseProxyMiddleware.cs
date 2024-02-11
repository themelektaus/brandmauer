using System.Diagnostics;
using System.Net;

using Yarp.ReverseProxy.Forwarder;

namespace Brandmauer;

public abstract class ReverseProxyMiddleware
{
    protected static bool TryGetTimeout(ReverseProxyRoute route, out TimeSpan timeout)
    {
        if (route.Timeout > 0)
        {
            timeout = TimeSpan.FromSeconds(route.Timeout);
            return true;
        }

        timeout = TimeSpan.Zero;
        return false;
    }

    protected static SocketsHttpHandler CreateHandler(TimeSpan timeout) => new()
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ConnectTimeout = timeout,
        ActivityHeadersPropagator = new ReverseProxyPropagator(
            DistributedContextPropagator.Current
        ),
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
