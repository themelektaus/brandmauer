using Microsoft.AspNetCore.Http.Extensions;

namespace Brandmauer;

public class CustomReverseProxyMiddleware(RequestDelegate next)
    : ReverseProxyMiddleware
{
    protected readonly HttpClient httpClient = new(handler);

    protected override RequestDelegate Next => next;

    protected override async Task OnPassAsync(HttpContext context)
    {
        var feature = context.Features.Get<ReverseProxyFeature>();

        if (feature.Route.UseYarp)
        {
            await NextAsync(context);
            return;
        }

        feature.Suffix += context.Request.QueryString;
        var url = new Uri($"{feature.Target}/{feature.Suffix.TrimStart('/')}");

        var request = new HttpRequestMessage
        {
            Method = GetMethod(context.Request),
            RequestUri = url,
            Content = GetContent(context.Request),
        };

        foreach (var header in context.Request.Headers)
        {
            if (!request.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value.ToArray()
            ))
            {
                request.Content?.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value.ToArray()
                );
            }
        }

        var ip = context.Connection.RemoteIpAddress.ToIp();
        var scheme = context.Request.Scheme;

        request.Headers.TryAddWithoutValidation("X-Real-IP", ip);
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", ip);
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", scheme);

        switch (feature.Route.HostModification)
        {
            case ReverseProxyRoute._HostModification.Unset:
                break;

            case ReverseProxyRoute._HostModification.Origin:
                request.Headers.Host = context.Request.Headers.Host;
                break;

            case ReverseProxyRoute._HostModification.Target:
                request.Headers.Host = url.Host;
                break;

            case ReverseProxyRoute._HostModification.Null:
                request.Headers.Host = null;
                break;
        }

        try
        {
            var content = request.Content is null
                ? default
                : await request.Content?.ReadAsStringAsync();

#if DEBUG
            var requestInfo = RequestInfo.Create(request, content);
#endif

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted
            );

            context.Response.StatusCode = (int) response.StatusCode;

            if (context.Response.StatusCode == 301)
                if (TryRedirect(context))
                    return;

            foreach (var header in response.Headers)
            {
                context.Response.Headers.TryAdd(
                    header.Key,
                    header.Value.ToArray()
                );
            }

            if (response.Content is not null)
            {
                var headers = context.Response.Headers;
                foreach (var header in response.Content.Headers.NonValidated)
                {
                    var key = header.Key;
                    headers[key] = Utils.Concat(headers[key], header.Value);
                }
            }

            context.Response.Headers.Remove("transfer-encoding");

            if (feature.Route.UseTeapot && response.StatusCode.HasErrorStatus())
            {
                context.Response.ContentLength = null;
                await NextAsync(context);
            }
            else
            {
                var contentStream = response.Content.ReadAsStream();
                await context.Response.Body.LoadFromAsync(contentStream);
#if DEBUG
                RequestInfo.Add(requestInfo, context.Response, contentStream);
#endif
            }
        }
        catch (Exception ex)
        {
            try
            {
                Console.WriteLine(ex.ToJson());
            }
            catch
            {
                Console.WriteLine(ex.ToString());
            }

            Console.WriteLine();

            if (ex is HttpRequestException _ex)
                if (_ex.HttpRequestError == HttpRequestError.ConnectionError)
                    if (TryRedirect(context))
                        return;

            context.Response.StatusCode = 500;
            await NextAsync(context);
        }
    }

    static bool TryRedirect(HttpContext context)
    {
        var location = context.Request.GetDisplayUrl();
        if (location.EndsWith('/'))
            return false;
        
        location += '/';
        Utils.Log("Redirect", location);
        context.Response.Redirect(
            location,
            permanent: false,
            preserveMethod: true
        );
        return true;
    }

    static HttpMethod GetMethod(HttpRequest request)
    {
        var method = request.Method;

        if (HttpMethods.IsHead(method))
            return HttpMethod.Head;

        if (HttpMethods.IsOptions(method))
            return HttpMethod.Options;

        if (HttpMethods.IsGet(method))
            return HttpMethod.Get;

        if (HttpMethods.IsPost(method))
            return HttpMethod.Post;

        if (HttpMethods.IsPut(method))
            return HttpMethod.Put;

        if (HttpMethods.IsDelete(method))
            return HttpMethod.Delete;

        if (HttpMethods.IsTrace(method))
            return HttpMethod.Trace;

        return new(method);
    }

    static StreamContent GetContent(HttpRequest request)
    {
        var method = request.Method;

        if (HttpMethods.IsGet(method))
            return default;

        if (HttpMethods.IsHead(method))
            return default;

        if (HttpMethods.IsDelete(method))
            return default;

        if (HttpMethods.IsTrace(method))
            return default;

        return new(request.Body);
    }
}
