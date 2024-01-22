using System.Text;

namespace Brandmauer;

public class RequestInfo
{
    static readonly object handle = new();
    static readonly List<RequestInfo> items = new();

    public static void Add(RequestInfo item)
    {
        lock (handle)
            items.Add(item);
    }

    public static List<RequestInfo> GetAll()
    {
        List<RequestInfo> items;
        lock (handle)
            items = [.. RequestInfo.items];
        return items;
    }

    public static void Clear()
    {
        lock (handle)
            items.RemoveAll(x => x.ResponseStatusCode != 0);
    }

    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Method { get; set; }
    public string Url { get; set; }
    public string Headers { get; set; }
    public string ContentHeaders { get; set; }
    public string Content { get; set; }

    public int ResponseStatusCode { get; set; }
    public string SessionId { get; set; }
    public string ResponseHeaders { get; set; }
    public string ResponseContentType { get; set; }
    public string ResponseContent { get; set; }

    public override string ToString()
    {
        var text = new StringBuilder();

        text.AppendLine($"[{Timestamp:HH:mm:ss}]");
        text.AppendLine(Method + " " + Url);

        if (!string.IsNullOrEmpty(Headers))
        {
            text.AppendLine("--");
            text.AppendLine(Headers);
            text.AppendLine();
        }

        if (!string.IsNullOrEmpty(ContentHeaders))
        {
            text.AppendLine("-- (Content Headers)");
            text.AppendLine(ContentHeaders);
            text.AppendLine();
        }

        if (!string.IsNullOrEmpty(Content))
        {
            text.AppendLine("-- (Body)");
            text.AppendLine(Content);
            text.AppendLine();
        }

        if (ResponseStatusCode != 0)
        {
            text.AppendLine($"[{ResponseStatusCode}]");
            text.AppendLine($"Session Id: {SessionId}");
            text.AppendLine(ResponseHeaders);
            text.AppendLine($"-");
            text.AppendLine($"{ResponseContent}");
            text.AppendLine($"-");
            text.AppendLine();
        }

        return text.ToString().TrimEnd();
    }

#if DEBUG
    public static RequestInfo Create(
        HttpRequestMessage request,
        string content
    )
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

    public static void Add(
        RequestInfo requestInfo,
        HttpResponse response,
        Stream contentStream
    )
    {
        Add(
            requestInfo,
            response.StatusCode,
            response.Headers.Select(
                x => new KeyValuePair<string, IEnumerable<string>>(
                    x.Key,
                    x.Value
                )
            ),
            response.ContentType,
            contentStream
        );
    }

    public static void Add(
        RequestInfo requestInfo,
        HttpResponseMessage response,
        Stream contentStream
    )
    {
        Add(
            requestInfo,
            (int) response.StatusCode,
            response.Headers,
            null,
            contentStream
        );
    }

    static void Add(
        RequestInfo requestInfo,
        int statusCode,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
        string contentType,
        Stream contentStream
    )
    {
        var content = contentStream.ReadString();

        if (requestInfo is null)
            return;

        requestInfo.ResponseStatusCode = statusCode;

        requestInfo.ResponseHeaders = headers
            .Select(x => $"{x.Key}: {x.Value.Join(',')}")
            .Join(Environment.NewLine)
            .Trim();

        if (contentType is not null)
            requestInfo.ResponseContentType = contentType;
        requestInfo.ResponseContent = content;

        Add(requestInfo);
    }
#endif
}
