namespace Brandmauer;

public class CustomReverseProxyMiddleware : ReverseProxyMiddleware
{
    readonly HttpClient httpClient = new(new HttpClientHandler()
    {
        ServerCertificateCustomValidationCallback
            = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    });

    protected override RequestDelegate Next { get; init; }

    public CustomReverseProxyMiddleware(RequestDelegate next)
    {
        Next = next;
    }

    protected override async Task OnPassAsync(HttpContext context)
    {
        if (Feature.Route.UseYarp)
        {
            await NextAsync(context);
            return;
        }

        context.Response.Headers.TryAdd("X-Reverse-Proxy", Utils.Name);

        Feature.Suffix += context.Request.QueryString;
        var url = new Uri($"{Feature.Target}/{Feature.Suffix.TrimStart('/')}");

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
        request.Headers.Add("X-Real-IP", ip);
        request.Headers.Add("X-Forwarded-For", ip);
        request.Headers.Host = url.Host;

        try
        {
            var content = request.Content is null
                ? default
                : await request.Content?.ReadAsStringAsync();

#if DEBUG
            var requestInfo = CreateRequestInfo(request, content);
#endif

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted
            );

            context.Response.StatusCode = (int) response.StatusCode;

            foreach (var header in response.Headers)
                context.Response.Headers.TryAdd(
                    header.Key,
                    header.Value.ToArray()
                );

            foreach (var header in response.Content.Headers)
                context.Response.Headers.TryAdd(
                    header.Key,
                    header.Value.ToArray()
                );

            context.Response.Headers.Remove("transfer-encoding");


            if (response.StatusCode.HasErrorStatus())
            {
                context.Response.ContentLength = null;
                await NextAsync(context);
            }
            else
            {
                var bodyStream = response.Content.ReadAsStream();
                await context.Response.Body.LoadFromAsync(bodyStream);
#if DEBUG
                AddRequestInfo(requestInfo, context, bodyStream);
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

            var path = context.Request.Path.ToString();
            if (!path.EndsWith('/'))
            {
                context.Response.Redirect(
                    $"{path}/",
                    permanent: false,
                    preserveMethod: true
                );
                return;
            }

            context.Response.StatusCode = 500;
            await NextAsync(context);
        }
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

#if DEBUG
    RequestInfo CreateRequestInfo(HttpRequestMessage request, string content)
    {
        var requestInfo = new RequestInfo()
        {
            Method = request.Method.ToString(),
            Url = request.RequestUri.ToString(),
            Headers = request.Headers.ToString().Trim(),
        };

        if (request.Content is not null)
        {
            var contentHeaders = request.Content.Headers.ToString().Trim();
            requestInfo.ContentHeaders = contentHeaders;
            requestInfo.Content = content;
        }

        return requestInfo;
    }

    static void AddRequestInfo(
        RequestInfo requestInfo,
        HttpContext context,
        Stream bodyStream
    )
    {
        if (requestInfo is null)
            return;

        requestInfo.ResponseStatusCode = context.Response.StatusCode;

        requestInfo.ResponseHeaders = context.Response.Headers
            .Select(x => $"{x.Key}: {x.Value}")
            .Join(Environment.NewLine)
            .Trim();

        requestInfo.ResponseContentType = context.Response.ContentType;
        requestInfo.ResponseContent = bodyStream.ReadString();

        RequestInfo.Add(requestInfo);
    }
#endif
}
