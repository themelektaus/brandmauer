namespace Brandmauer.LiveCode;

public enum RunnerResultStatus
{
    Unknown = -1,
    OK = 0,
    AssemblyNotFound = 10,
    ClassNotFound = 11,
    MethodNotFound = 12,
    Canceled = 20,
    Timeout = 31,
    Exception = 98
}
