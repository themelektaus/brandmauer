using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;

using Microsoft.AspNetCore.Http.Extensions;

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

    protected static void SetForwardedHeaders(
        HttpContext context,
        HttpRequestHeaders headers
    )
    {
        var ip = context.Connection.RemoteIpAddress.ToIp();
        var scheme = context.Request.Scheme;
        var source = context.Features.Get<ReverseProxyFeature>().Source;

        SetHeader(headers, "X-Real-IP", ip);
        SetHeader(headers, "X-Forwarded-For", ip);
        SetHeader(headers, "X-Forwarded-Proto", scheme);
        SetHeader(headers, "X-Source", source);
    }

    static void SetHeader(HttpRequestHeaders headers, string name, string value)
    {
        headers.Remove(name);
        headers.TryAddWithoutValidation(name, value);
    }

    // The audit lives in memory and is only trimmed every 6 h, so an
    // unreachable upstream under load or a stream of aborted requests must
    // not flood it: the same error of a route is written at most once per
    // ERROR_INTERVAL, cancellations of all routes once per CANCEL_INTERVAL,
    // each with the number of occurrences held back since.
    static readonly TimeSpan ERROR_INTERVAL = TimeSpan.FromMinutes(1);
    static readonly TimeSpan CANCEL_INTERVAL = TimeSpan.FromMinutes(10);

    class Throttle
    {
        public DateTime next = DateTime.MinValue;
        public int held;

        public bool TryPass(TimeSpan interval, out int held)
        {
            lock (this)
            {
                var now = DateTime.Now;
                if (now < next)
                {
                    this.held++;
                    held = 0;
                    return false;
                }

                next = now + interval;
                held = this.held;
                this.held = 0;
                return true;
            }
        }
    }

    static readonly ConcurrentDictionary<string, Throttle> errorThrottles = new();
    static readonly Throttle cancelThrottle = new();

    // Short type for the audit; the class name does not fit its type column.
    protected abstract string AuditType { get; }

    protected void AuditError(
        HttpContext context,
        string error,
        Exception exception,
        string extra = null
    )
    {
        var feature = context.Features.Get<ReverseProxyFeature>();
        var routeId = feature?.Route?.Identifier.Id;

        var key = $"{routeId}|{error}|{exception?.GetType().Name}";
        var throttle = errorThrottles.GetOrAdd(key, _ => new());
        if (!throttle.TryPass(ERROR_INTERVAL, out var held))
            return;

        var message = new StringBuilder();
        message.Append($"{error} {Describe(context)}");
        if (!string.IsNullOrEmpty(extra))
            message.Append($" {extra}");

        for (var ex = exception; ex is not null; ex = ex.InnerException)
            message.Append($" | {ex.GetType().Name}: {ex.Message}");

        if (held > 0)
            message.Append($" (+{held} more since last entry)");

        Audit.Error(AuditType, message.ToString());
    }

    protected void AuditCanceled(HttpContext context, string reason)
    {
        if (!cancelThrottle.TryPass(CANCEL_INTERVAL, out var held))
            return;

        var message = $"Canceled: {reason} {Describe(context)}";
        if (held > 0)
            message += $" (+{held} more since last entry)";

        Audit.Info(AuditType, message);
    }

    static string Describe(HttpContext context)
    {
        var request = context.Request;
        var feature = context.Features.Get<ReverseProxyFeature>();
        var route = feature?.Route;

        return $"{request.Method} {request.GetDisplayUrl()} → " +
            $"{feature?.Target}/{feature?.Suffix?.TrimStart('/')} " +
            $"(Route {route?.Identifier.Id}, Source {feature?.Source})";
    }
}
