using Microsoft.AspNetCore.Http.Json;

using Brandmauer;

Database.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Default", LogLevel.Information);
#if RELEASE
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Error);
#else
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
#endif

builder.Services.Configure<JsonOptions>(options =>
{
    var serializer = options.SerializerOptions;
    serializer.IncludeFields = true;
    serializer.Converters.Add(new ExceptionConverter());
});

builder.WebHost.UseKestrel(options =>
{
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

        x.UseConnectionLogging();
    });
});

var app = builder.Build();

app.Urls.Add($"http://0.0.0.0:{Utils.HTTP}");
app.Urls.Add($"https://0.0.0.0:{Utils.HTTPS}");

Brandmauer.Endpoint.MapAll(app);

app.UseMiddleware<WellKnownMiddleware>();

app.UseReverseProxy();

app.UseTeapot("wwwroot/418.html", statusCodeBreakers: [404], x =>
{
    x.Spill(400, "Bad Request");
    x.Spill(404, "Not Found");
    x.Spill(503, "Service Unavailable");
});

app.UseMiddleware<FrontendMiddleware>();

#if RELEASE
app.RunInBackground<Updater>();
#endif

app.RunInBackground<DatabaseReloader>();

await app.RunAsync();

#if RELEASE
await app.DisposeAllIntervalTasksAsync();
#endif
