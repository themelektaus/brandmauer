using System.Net.Http.Headers;

namespace Brandmauer;

public abstract class ReverseProxyMiddleware
{
    protected abstract RequestDelegate Next { get; }

    protected abstract Task OnPassAsync(HttpContext context);

    public async Task Invoke(HttpContext context)
    {
        Utils.LogBegin(GetType(), context);

        var feature = context.Features.Get<ReverseProxyFeature>();
        if (feature is null || feature.UseScript)
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

    protected static void SetForwardedHeaders(
        HttpContext context,
        HttpRequestHeaders headers
    )
    {
        var ip = context.Connection.RemoteIpAddress.ToIp();
        var scheme = context.Request.Scheme;
        var source = context.Features.Get<ReverseProxyFeature>().Source;

        SetHeader(headers, "X-Real-IP", ip);
        SetHeader(headers, "X-Forwarded-For", ip);
        SetHeader(headers, "X-Forwarded-Proto", scheme);
        SetHeader(headers, "X-Source", source);
    }

    static void SetHeader(HttpRequestHeaders headers, string name, string value)
    {
        headers.Remove(name);
        headers.TryAddWithoutValidation(name, value);
    }
}
