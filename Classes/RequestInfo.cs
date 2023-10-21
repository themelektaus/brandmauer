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
            items = RequestInfo.items.ToList();
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
}
