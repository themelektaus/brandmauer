﻿using Microsoft.AspNetCore.Mvc;

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
#if LINUX
        app.MapGet($"{API}/update/download", Update.Download);
        app.MapGet($"{API}/update/install", Update.Install);
        app.MapGet($"{API}/update/wwwroot", Update.WwwRoot);
#endif

        app.MapGet($"{API}/config", Config.Get);
        app.MapPut($"{API}/config", Config.Set);

        app.MapGet($"{API}/hosts"/*?format=<json|html>*/, Hosts.GetAll);
        app.MapGet($"{API}/hosts/{{id}}", Hosts.Get);
        app.MapPost($"{API}/hosts", Hosts.Post);
        app.MapPut($"{API}/hosts", Hosts.Put);
        app.MapDelete($"{API}/hosts/{{id}}", Hosts.Delete);

#if DEBUG || LINUX
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
#endif

#if LINUX
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
#endif

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
        app.MapPut(
            $"{API}/reverseproxyroutes/script",
            ReverseProxyRoutes.PutScript
        );
        app.MapDelete(
            $"{API}/reverseproxyroutes/{{id}}",
            ReverseProxyRoutes.Delete
        );
        app.MapGet(
            $"{API}/reverseproxyroutes/requests",
            WhitelistMiddleware.GetPendingRequests
        );
        app.MapGet(
            $"{API}/reverseproxyroutes/requests/clear",
            WhitelistMiddleware.ClearPendingRequests
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

        app.MapGet($"{API}/authentications", Authentications.GetAll);
        app.MapGet($"{API}/authentications/{{id}}", Authentications.Get);
        app.MapPost($"{API}/authentications", Authentications.Post);
        app.MapPut($"{API}/authentications", Authentications.Put);
        app.MapDelete($"{API}/authentications/{{id}}", Authentications.Delete);

        app.MapGet($"{API}/smtpconnections", SmtpConnections.GetAll);
        app.MapGet($"{API}/smtpconnections/{{id}}", SmtpConnections.Get);
        app.MapPost($"{API}/smtpconnections", SmtpConnections.Post);
        app.MapPut($"{API}/smtpconnections", SmtpConnections.Put);
        app.MapDelete($"{API}/smtpconnections/{{id}}", SmtpConnections.Delete);

        app.MapPost(
            $"{API}/smtpconnections/send",
            async (HttpContext context) =>
            {
                return await SmtpConnections.SendAsync(context);
            }
        );

        app.MapGet($"{API}/monitors", Monitors.GetAll);
        app.MapGet($"{API}/monitors/{{id}}", Monitors.Get);
        app.MapPost($"{API}/monitors", Monitors.Post);
        app.MapPut($"{API}/monitors", Monitors.Put);
        app.MapDelete($"{API}/monitors/{{id}}", Monitors.Delete);

        app.MapGet($"{API}/pushlisteners", PushListeners.GetAll);
        app.MapGet($"{API}/pushlisteners/{{id}}", PushListeners.Get);
        app.MapPost($"{API}/pushlisteners", PushListeners.Post);
        app.MapPut($"{API}/pushlisteners", PushListeners.Put);
        app.MapDelete($"{API}/pushlisteners/{{id}}", PushListeners.Delete);

        app.MapGet($"{API}/dynamicdnshosts", DynamicDnsHosts.GetAll);
        app.MapGet($"{API}/dynamicdnshosts/{{id}}", DynamicDnsHosts.Get);
        app.MapPost($"{API}/dynamicdnshosts", DynamicDnsHosts.Post);
        app.MapPut($"{API}/dynamicdnshosts", DynamicDnsHosts.Put);
        app.MapDelete($"{API}/dynamicdnshosts/{{id}}", DynamicDnsHosts.Delete);

        app.MapGet($"{API}/shares", Shares.GetAll);
        app.MapGet($"{API}/shares/{{id}}", Shares.Get);
        app.MapPut($"{API}/shares", Shares.Put);
        app.MapDelete($"{API}/shares/{{id}}", Shares.Delete);

        app.MapGet($"{API}/resolve"/*?host={{host}}*/, Resolve);
        app.MapGet($"{API}/whatsmyip", WhatsMyIp);
        app.MapGet($"{API}/time", Time);

        app.MapPost(
            $"{API}/run",
            async (HttpContext context) =>
            {
                return await RunAsync(context);
            }
        );

        app.MapGet($"{API}/audit"/*?limit={{limit}}*/, Audit.Get);
        app.MapGet($"{API}/audit/{{id}}"/*?limit={{limit}}*/, Audit.GetById);

        app.MapGet(
            $"{API}/namecom/domains",
            async (HttpContext context) =>
            {
                return await NameCom.GetDomains(context);
            }
        );
        app.MapGet(
            $"{API}/namecom/domains/{{domain}}/records",
            async (HttpContext context, string domain) =>
            {
                return await NameCom.GetDomainRecords(context, domain);
            }
        );

#if FORTI && (DEBUG || LINUX)
        app.MapGet($"{API}/fortigate/connect", FortiClient.Connect);
        app.MapGet($"{API}/fortigate/disconnect", FortiClient.Disconnect);
        app.MapGet($"{API}/fortigate/disconnect-if-reconnecting", FortiClient.DisconnectIfReconnecting);
        app.MapGet($"{API}/fortigate/reconnect", FortiClient.Reconnect);
        app.MapGet($"{API}/fortigate/status", FortiClient.Status);
        app.MapGet($"{API}/fortigate/update", FortiClient.Update);
#endif
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
        host ??= string.Empty;

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

        if (!headers.TryGetValue("X-Real-IP", out var ip))
            if (!headers.TryGetValue("X-Forwarded-For", out ip))
                ip = context.Connection.RemoteIpAddress.ToIp();

        return Results.Text(ip);
    }

    static IResult Time(HttpContext context)
    {
        return Results.Text(DateTimeOffset.Now.ToUnixTimeSeconds().ToString());
    }

    static async Task<IResult> RunAsync(HttpContext context)
    {
        var sourceCode = await context.Request.Body.ReadStringAsync();
        var result = await LiveCodeMiddleware.ExecuteAsync(sourceCode, context);
        return Results.Text(result.ToString());
    }
}
