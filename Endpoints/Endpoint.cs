using Microsoft.AspNetCore.Mvc;

namespace Brandmauer;

public static partial class Endpoint
{
    public static void MapAll(WebApplication app)
    {
        const string API = "api";

        app.MapGet(API, Api);

        app.MapGet($"{API}/info", Info.Get);
        app.MapGet($"{API}/info/requests", Info.GetRequests);

        app.MapGet($"{API}/update", Update.Check);
        app.MapGet($"{API}/update/download", Update.Download);
        app.MapGet($"{API}/update/install", Update.Install);

        app.MapGet($"{API}/config", Config.Get);
        app.MapPut($"{API}/config", Config.Set);

        app.MapGet($"{API}/hosts", Hosts.GetAll);
        app.MapGet($"{API}/hosts/{{id}}", Hosts.Get);
        app.MapPost($"{API}/hosts", Hosts.Post);
        app.MapPut($"{API}/hosts", Hosts.Put);
        app.MapDelete($"{API}/hosts/{{id}}", Hosts.Delete);

        app.MapGet($"{API}/services", Services.GetAll);
        app.MapGet($"{API}/services/{{id}}", Services.Get);
        app.MapPost($"{API}/services", Services.Post);
        app.MapPut($"{API}/services", Services.Put);
        app.MapDelete($"{API}/services/{{id}}", Services.Delete);

        app.MapGet($"{API}/rules", Rules.GetAll);
        app.MapGet($"{API}/rules/{{id}}", Rules.Get);
        app.MapPost($"{API}/rules", Rules.Post);
        app.MapPut($"{API}/rules", Rules.Put);
        app.MapDelete($"{API}/rules/{{id}}", Rules.Delete);

        app.MapGet($"{API}/natroutes", NatRoutes.GetAll);
        app.MapGet($"{API}/natroutes/{{id}}", NatRoutes.Get);
        app.MapPost($"{API}/natroutes", NatRoutes.Post);
        app.MapPut($"{API}/natroutes", NatRoutes.Put);
        app.MapDelete($"{API}/natroutes/{{id}}", NatRoutes.Delete);

        app.MapGet(
            $"{API}/iptables"/*?output=<script|stdout|stderr|data>*/,
            IpTables.Get
        );
        app.MapGet(
            $"{API}/build/preview",
            Build.Preview
        );
        app.MapGet(
            $"{API}/build/dirty",
            Build.Dirty
        );
        app.MapGet(
            $"{API}/build/apply"/*?output=<script|stdout|stderr|data>*/,
            Build.Apply
        );
        app.MapGet(
            $"{API}/build/clear"/*?output=<script|stdout|stderr|data>*/,
            Build.Clear
        );

        app.MapGet(
            $"{API}/reverseproxyroutes",
            ReverseProxyRoutes.GetAll
        );
        app.MapGet(
            $"{API}/reverseproxyroutes/{{id}}",
            ReverseProxyRoutes.Get
        );
        app.MapPost(
            $"{API}/reverseproxyroutes",
            ReverseProxyRoutes.Post
        );
        app.MapPut(
            $"{API}/reverseproxyroutes",
            ReverseProxyRoutes.Put
        );
        app.MapDelete(
            $"{API}/reverseproxyroutes/{{id}}",
            ReverseProxyRoutes.Delete
        );

        app.MapGet($"{API}/certificates", Certificates.GetAll);
        app.MapGet($"{API}/certificates/{{id}}", Certificates.Get);
        app.MapPost($"{API}/certificates", Certificates.Post);
        app.MapPut($"{API}/certificates", Certificates.Put);
        app.MapDelete($"{API}/certificates/{{id}}", Certificates.Delete);

        app.MapGet(
            $"{API}/certificates/export"
            /**/,
            Certificates.ExportAll
        );
        app.MapGet(
            $"{API}/certificates/export/clear"
            /**/,
            Certificates.ClearAllExports
        );
        app.MapGet(
            $"{API}/certificates/{{id}}/update"
            /*?letsEncrypt={{letsEncrypt}}&staging={{staging}}*/,
            Certificates.Update
        );
        app.MapGet(
            $"{API}/certificates/{{id}}/download"
            /*?format={{format}}*/,
            Certificates.Download
        );

        app.MapGet($"api/resolve"/*?host={{host}}*/, Resolve);
        app.MapGet($"api/whatsmyip", WhatsMyIp);
    }

    static IResult Api(IEnumerable<EndpointDataSource> endpointSources)
    {
        var endpoints = new List<dynamic>();

        foreach (var ep in endpointSources.SelectMany(x => x.Endpoints))
        {
            string path = default;
            if (ep is RouteEndpoint rep)
                path = rep.RoutePattern.RawText;

            var methods = ep.Metadata.OfType<HttpMethodMetadata>()
                .FirstOrDefault()?.HttpMethods;

            foreach (var method in methods)
                endpoints.Add(new { method, path, });
        }

        return Results.Json(new { endpoints });
    }

    static IResult Resolve([FromQuery] string host)
    {
        host ??= "";

        foreach (var part in host.Split(','))
            if (!Utils.IsIpAddress(part.Split('/')[0]))
                goto Continue;

        return Results.Text(host);

    Continue:
        if (Utils.TryGetIpAddress(host, out var ipAddress))
            return Results.Text(ipAddress);

        return Results.NoContent();
    }

    static IResult WhatsMyIp(HttpContext context)
    {
        var headers = context.Request.Headers;
        if (!headers.TryGetValue("X-Forwarded-For", out var ip))
            ip = context.Connection.RemoteIpAddress.ToIp();
        return Results.Text(ip);
    }
}
