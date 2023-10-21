namespace Brandmauer;

public abstract class ReverseProxyMiddleware
{
    protected abstract RequestDelegate Next { get; init; }

    protected abstract Task OnPassAsync(HttpContext context);

    protected ReverseProxyFeature Feature { get; private set; }

    public async Task Invoke(HttpContext context)
    {
        Utils.LogIn(GetType(), context);

        Feature = context.Features.Get<ReverseProxyFeature>();
        if (Feature is null || Feature.Target is null)
        {
            await NextAsync(context);
            return;
        }

        await OnPassAsync(context);
    }

    protected async Task NextAsync(HttpContext context)
    {
        await Next(context);

        Utils.LogOut(GetType(), context);
    }
}
