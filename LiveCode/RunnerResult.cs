namespace Brandmauer.LiveCode;

public struct RunnerResult
{
    public object returnValue;
    public RunnerResultStatus status;

    public RunnerResult Status(RunnerResultStatus status)
    {
        this.status = status;
        return this;
    }
}
