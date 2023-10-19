using Microsoft.AspNetCore.Http.Extensions;

using System.Net.NetworkInformation;

using System.Reflection;

using System.Text.RegularExpressions;

namespace Brandmauer;

public static partial class Utils
{
#if RELEASE
    public const ushort HTTP = 80;
    public const ushort HTTPS = 443;
#else
    public const ushort HTTP = 5080;
    public const ushort HTTPS = 5443;
#endif

    public static string Name
        => GetAssemblyName().Name;

    public static AssemblyName GetAssemblyName()
        => Assembly.GetExecutingAssembly().GetName();

    [GeneratedRegex("^(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$")]
    private static partial Regex IPv4Regex();

    public static readonly string[] allLocalIpAddresses = EnumerateAllLocalIpAddresses().ToArray();

    public static bool IsIpAddress(string host)
    {
        return IPv4Regex().IsMatch(host);
    }

    public static IEnumerable<string> EnumerateAllLocalIpAddresses()
    {
        foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            foreach (var address in @interface.GetIPProperties().UnicastAddresses)
                yield return address.Address.ToString();
    }

    public static bool TryGetIpAddress(string address, out string ipAddress)
    {
        if (address is null)
            goto Error;

        ipAddress = address.ToIpAddress();

        if (IsIpAddress(ipAddress))
            return true;

        Error:
        ipAddress = null;
        return false;
    }

#if DEBUG
    static readonly object logHandle = new();
    static int logIndex;
    static int lastLogIndex;
    static readonly Dictionary<string, int> logIndexDict = new();
#endif

    public static void Log<T>(HttpContext context, string message)
    {
#if DEBUG
        lock (logHandle)
        {
            var index = GetLogIndex(context);
            Console.WriteLine($" [{index}] {message}");
        }
#endif
    }

    public static void LogIn<T>(HttpContext context)
    {
#if DEBUG
        Log<T>(context, $"{context.Response.StatusCode} > {typeof(T)} {context.Request.Method} {context.Request.GetDisplayUrl()}");
#endif
    }

    public static void LogOut<T>(HttpContext context)
    {
#if DEBUG
        Log<T>(context, $"{typeof(T)} > {context.Response.StatusCode}");
#endif
    }

#if DEBUG
    static int GetLogIndex(HttpContext context)
    {
        var key = context.TraceIdentifier;
        if (!logIndexDict.TryGetValue(key, out var index))
        {
            index = ++logIndex;
            logIndexDict.Add(key, index);
        }
        if (lastLogIndex != index)
        {
            lastLogIndex = index;
            Console.WriteLine();
        }
        return index;
    }
#endif
}
