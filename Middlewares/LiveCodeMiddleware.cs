using Brandmauer.LiveCode;

namespace Brandmauer;

public class LiveCodeMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var feature = context.Features.Get<ReverseProxyFeature>();

        if (feature is not null && feature.UseScript)
        {
            context.Features.Set(new PermissionFeature { Authorized = true });

            var response = context.Response;

            switch (feature.Route.ScriptOutputType)
            {
                case ReverseProxyRoute._ScriptOutputType.Text:
                    response.ContentType = "text/plain";
                    break;

                case ReverseProxyRoute._ScriptOutputType.Json:
                    response.ContentType = "application/json";
                    break;

                case ReverseProxyRoute._ScriptOutputType.Html:
                    response.ContentType = "text/html";
                    break;
            }

            var result = await ExecuteAsync(feature.Route.Script, context);

            await response.Body.LoadFromAsync(result.ToString());

            return;
        }

        await next.Invoke(context);
    }

    public static async Task<LiveCodeResult> ExecuteAsync(
        string sourceCode,
        params object[] args
    )
    {
        var compiler = new CSharpCompiler { sourceCode = sourceCode };

        var compilerResult = await compiler.CompileAsync();

        if (compilerResult.HasErrors())
        {
            return new()
            {
                sourceCode = sourceCode,
                compilerErrors = compilerResult.errors
                    .Select(x => $"{x.message} ({x.line})")
                    .ToArray()
            };
        }

        using var runner = new Runner(compilerResult);

        var runnerResult = await runner.ExecuteAsync(args);

        return new()
        {
            sourceCode = sourceCode,
            runnerResult = runnerResult
        };
    }
}
