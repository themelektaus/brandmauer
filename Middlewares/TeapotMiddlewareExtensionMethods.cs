namespace Brandmauer;

public static class TeapotMiddlewareExtensionMethods
{
    public static void UseTeapot(
        this IApplicationBuilder @this,
        string htmlFile,
        int[] statusCodeBreakers,
        Action<TeapotMiddleware.Configuration> setup
    )
    {
        @this.UseMiddleware<TeapotMiddleware>(
            htmlFile,
            statusCodeBreakers,
            setup
        );
    }
}
