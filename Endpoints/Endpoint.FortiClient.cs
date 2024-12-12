#if DEBUG || LINUX
namespace Brandmauer;

public static partial class Endpoint
{
    public static class FortiClient
    {
        static readonly ShellCommand.RemoteOptions remoteOptions
#if DEBUG
            = new("nockal.com", "app", "ibidgHbne2tWX8NWjAcDEsdg8kzCvJ4r");
        const string HOME = "/home/app";
#else
            = null;
        const string HOME = "/root";
#endif

        public static IResult Connect(HttpRequest request)
        {
            return ShellCommand.Execute($"bash {HOME}/ck-connect.sh", remoteOptions)
                .ToResult(request);
        }

        public static IResult Disconnect(HttpRequest request)
        {
            return ShellCommand.Execute($"bash {HOME}/ck-disconnect.sh", remoteOptions)
                .ToResult(request);
        }

        public static IResult DisconnectIfReconnecting(HttpRequest request)
        {
            return ShellCommand.Execute($"bash {HOME}/ck-disconnect-if-reconnecting.sh", remoteOptions)
                .ToResult(request);
        }

        public static IResult Reconnect(HttpRequest request)
        {
            return ShellCommand.Execute($"bash {HOME}/ck-reconnect.sh", remoteOptions)
                .ToResult(request);
        }

        public static IResult Status(HttpRequest request)
        {
            var result = ShellCommand.Execute($"bash {HOME}/ck-status.sh", remoteOptions);

            if (request.Query.TryGetValue("format", out var format) && format == "json")
            {
                var status = result.StdOut?
                    .Split('\n')
                    .FirstOrDefault(x => x.StartsWith("Status: "))
                    ?[8..] ?? "Unknown";

                return Results.Json(new { status });
            }

            return result.ToResult(request);
        }

        public static IResult Update(HttpRequest request)
        {
            return ShellCommand.Execute($"bash {HOME}/ck-update.sh", remoteOptions)
                .ToResult(request);
        }
    }
}
#endif
