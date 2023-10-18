namespace Brandmauer;

public static class ReverseProxyMiddlewareExtensionMethods
{
	public static void UseReverseProxy(this IApplicationBuilder @this)
	{
		@this.UseMiddleware<ReverseProxyMiddleware>((ReverseProxyMiddleware.Settings settings) =>
		{
			Database.Use(x =>
			{
				settings.logging = x.Config.ReverseProxyLogging;
                settings.routes = x.ReverseProxyRoutes.Where(x => x.Enabled).ToList();
			});
		});
	}
}
