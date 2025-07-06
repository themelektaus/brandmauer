#if LINUX
namespace Brandmauer;

public static partial class Endpoint
{
    public static class SystemInfo
    {
        public static IResult Get(HttpRequest request)
        {
            var sep = Enumerable.Repeat('-', 67).Join();

            string Echo(string command)
            {
                return @$"
                    echo ""{sep}""
                    echo "" {command}""
                    echo ""{sep}""
                    {command}
                    echo """"
                    echo """"
                ";
            }

            var result = ShellCommand.Execute(@$"
                {Echo("uname -a")}
                {Echo("nvme smart-log /dev/nvme0n1")}
                {Echo("lscpu")}
                {Echo("lspci -v")}
                {Echo("lsusb -v")}
            ");

            return result.ToResult(request);
        }
    }
}
#endif
