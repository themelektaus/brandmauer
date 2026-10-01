using System.Net.Security;
using System.Security.Authentication;

using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Server.Kestrel.Https;

#if WINDOWS && RELEASE
using Microsoft.Extensions.Hosting.WindowsServices;
#endif

using Brandmauer;

#if WINDOWS && RELEASE
Environment.CurrentDirectory = Path.GetDirectoryName(Environment.ProcessPath);
#endif

Database.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Default", LogLevel.Information);

#if DEBUG
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
#else
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
#endif

builder.Logging.AddFilter(
    "Yarp.ReverseProxy.Forwarder.HttpForwarder",
    LogLevel.Warning
);

builder.Services.AddHttpForwarder();

builder.Services.Configure<JsonOptions>(options =>
{
    var serializer = options.SerializerOptions;
    serializer.IncludeFields = true;
    serializer.Converters.Add(new ExceptionConverter());
});

builder.WebHost.UseKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;

    options.ListenAnyIP(Utils.HTTP);
    options.ListenAnyIP(Utils.HTTPS, x =>
    {
        x.UseHttps(new TlsHandshakeCallbackOptions
        {
            OnConnection = context =>
            {
                var sni = context.ClientHelloInfo.ServerName;

                SslStreamCertificateContext certificateContext = null;

                if (
                    !string.IsNullOrEmpty(sni) &&
                    !Utils.allLocalIpAddresses.Contains(sni) &&
                    !Utils.IsIpAddress(sni)
                )
                    certificateContext = Certificate.GetContext(sni);

                if (certificateContext is null)
                    throw new AuthenticationException($"No certificate for '{sni}'");

                return ValueTask.FromResult(new SslServerAuthenticationOptions
                {
                    ServerCertificateContext = certificateContext
                });
            }
        });

#if DEBUG
        x.UseConnectionLogging();
#endif
    });
});

#if WINDOWS && RELEASE
builder.Host.UseWindowsService();
#endif

var app = builder.Build();

app.Urls.Add($"http://0.0.0.0:{Utils.HTTP}");
app.Urls.Add($"https://0.0.0.0:{Utils.HTTPS}");

Brandmauer.Endpoint.MapAll(app);

foreach (var x in new[] {
#if DEBUG
    typeof(HelloWorldMiddleware),
#endif
    typeof(WellKnownMiddleware),
    typeof(LoginMiddleware),
    typeof(WhitelistMiddleware),
    typeof(PushMiddleware),
    typeof(ShareMiddleware),
    typeof(IconMiddleware),
    typeof(ReverseProxyPreparatorMiddleware),
    typeof(YarpReverseProxyMiddleware),
    typeof(CustomReverseProxyMiddleware),
    typeof(LiveCodeMiddleware),
    typeof(FrontendMiddleware),
    typeof(TeapotMiddleware),
}) app.UseMiddleware(x);

foreach (var x in new[] {
    typeof(IntervalTask_Continuously),
    typeof(IntervalTask_Daily),
    typeof(IntervalTask_DnsServer),
    typeof(IntervalTask_ReloadDatabase),
#if LINUX
    typeof(IntervalTask_UpdateBrandmauer),
    typeof(IntervalTask_RenewCertifcates),
    typeof(IntervalTask_Startup),
#endif
#if LINUX && FORTI
    typeof(IntervalTask_FortiClient),
#endif

}) app.RunInBackground(x);

await app.RunAsync();
await app.DisposeAllIntervalTasksAsync();

Audit.Save();
