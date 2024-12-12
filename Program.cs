using Microsoft.AspNetCore.Http.Json;

#if WINDOWS
using Microsoft.Extensions.Hosting.WindowsServices;
#endif

using Brandmauer;

#if WINDOWS
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
        x.UseHttps(x =>
        {
            x.ServerCertificateSelector = (_, sni) =>
            {
                if (string.IsNullOrEmpty(sni))
                    return null;

                if (Utils.allLocalIpAddresses.Contains(sni))
                    return null;

                if (Utils.IsIpAddress(sni))
                    return null;

                return Certificate.Get(sni)?.Pfx;
            };
        });

#if DEBUG
        x.UseConnectionLogging();
#endif
    });
});

#if WINDOWS
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
    typeof(IntervalTask_FortiClient),
#endif
}) app.RunInBackground(x);

await app.RunAsync();
await app.DisposeAllIntervalTasksAsync();

Audit.Save();
