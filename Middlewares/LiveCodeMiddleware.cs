using Brandmauer.LiveCode;

namespace Brandmauer;

public class LiveCodeMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var feature = context.Features.Get<ReverseProxyFeature>();

        if (feature is not null && feature.UseScript)
        {
            var result = await ExecuteAsync(feature.Route.Script);

            context.Features.Set(new PermissionFeature { Authorized = true });

            var response = context.Response;
            response.ContentType = "text/html";
            await response.Body.LoadFromAsync(result.ToString());

            return;
        }

        await next.Invoke(context);
    }

    static async Task<LiveCodeResult> ExecuteAsync(string sourceCode)
    {
        var compiler = new CSharpCompiler { sourceCode = sourceCode };

        var compilerResult = await compiler.CompileAsync();

        if (compilerResult.HasErrors())
        {
            return new()
            {
                sourceCode = sourceCode,
                compilerErrors = compilerResult.errors
                    .Select(x => $"[Line: {x.line}] {x.message}")
                    .ToArray()
            };
        }

        using var runner = new Runner(compilerResult);
        
        var runnerResult = await runner.ExecuteAsync(CancellationToken.None);

        return new()
        {
            sourceCode = sourceCode,
            runnerResult = runnerResult
        };
    }
}
