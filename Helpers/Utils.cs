using Microsoft.AspNetCore.Http.Extensions;

using Microsoft.Extensions.Primitives;

using System.Net.Http.Headers;
using System.Net.NetworkInformation;

using System.Reflection;

using System.Text.RegularExpressions;

using F = System.Reflection.BindingFlags;

namespace Brandmauer;

public static partial class Utils
{
    public static readonly string NL = Environment.NewLine;

#if !DEBUG && !WINDOWS
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

    [GeneratedRegex("(?<=[a-z])([A-Z])")]
    public static partial Regex CamelSpaceRegex();

    const string IP_PART = "(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)";

    [GeneratedRegex($"^{IP_PART}\\.{IP_PART}\\.{IP_PART}\\.{IP_PART}")]
    private static partial Regex IPv4Regex();

    public static readonly string[] allLocalIpAddresses
        = EnumerateAllLocalIpAddresses().ToArray();

    public static bool IsIpAddress(string host)
    {
        return IPv4Regex().IsMatch(host);
    }

    public static IEnumerable<string> EnumerateAllLocalIpAddresses()
    {
        foreach (var @if in NetworkInterface.GetAllNetworkInterfaces())
            foreach (var address in @if.GetIPProperties().UnicastAddresses)
                yield return address.Address.ToString();
    }

    public static bool TryGetIpAddress(string address, out string ipAddress)
    {
        if (address is null)
            goto Error;

        ipAddress = address.ToIpAddress();

        if (ipAddress is not null && IsIpAddress(ipAddress))
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

    public static void Log(string category, string message)
    {
#if DEBUG
        lock (logHandle)
        {
            Console.WriteLine($" [{category}] {message}");
        }
#endif
    }

    public static void Log(HttpContext context, string message)
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
        LogIn(typeof(T), context);
#endif
    }

    public static void LogIn(Type type, HttpContext context)
    {
#if DEBUG
        var statusCode = context.Response.StatusCode;
        var method = context.Request.Method;
        var url = context.Request.GetDisplayUrl();
        Log(context, $"{statusCode} > {type.Name} {method} {url}");
#endif
    }

    public static void LogOut<T>(HttpContext context)
    {
#if DEBUG
        LogOut(typeof(T), context);
#endif
    }

    public static void LogOut(Type type, HttpContext context)
    {
#if DEBUG
        Log(context, $"{type.Name} > {context.Response.StatusCode}");
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

    //https://github.com/microsoft/reverse-proxy/blob/4e32b6b87af17ed1e60bb84aa76d8585c7e5c11f/src/ReverseProxy/Forwarder/RequestUtilities.cs#L350
    public static StringValues Concat(in StringValues array, in HeaderStringValues values)
    {
        if (values.Count <= 1)
            return StringValues.Concat(array, values.ToString());

        var count = array.Count;
        var result = new string[count + values.Count];

        if (count == 1)
            result[0] = array.ToString();
        else
            array.ToArray().CopyTo(result, 0);

        foreach (var value in values)
            result[count++] = value;

        return result;
    }

    public static IConfigurationRoot GetAppSettings()
    {
        if (
            !File.Exists(
#if DEBUG
                "appsettings.Development.json"
#else
                "appsettings.json"
#endif
            )
        )
        {
            return null;
        }

        IConfigurationBuilder builder = new ConfigurationBuilder();

        return builder.AddJsonFile(
#if DEBUG
            "appsettings.Development.json"
#else
            "appsettings.json"
#endif
        ).Build();
    }

    public static List<MethodInfo> GetMethodsByAttribute<T>()
        where T : Attribute
    {
        var methods = new List<MethodInfo>();

        var types = typeof(Utils).Assembly.GetTypes();
        var flags = F.Public | F.NonPublic | F.Static;

        foreach (var type in types)
        {
            var methodInfos = type.GetMethods(flags);
            foreach (var method in methodInfos)
            {
                var attribute = method.GetCustomAttribute<FrontendAttribute>();
                if (attribute is not null)
                    methods.Add(method);
            }
        }

        return methods;
    }

    public static List<MethodInfo> GetInstanceMethods()
    {
        var methods = new List<MethodInfo>();

        var types = typeof(Utils).Assembly.GetTypes();
        var flags = F.Public | F.NonPublic | F.Instance;

        foreach (var type in types)
        {
            var methodInfos = type.GetMethods(flags);
            foreach (var method in methodInfos)
                methods.Add(method);
        }

        return methods;
    }
}
