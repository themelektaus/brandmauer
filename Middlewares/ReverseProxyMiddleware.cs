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
}
