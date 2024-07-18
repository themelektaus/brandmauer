namespace Brandmauer.LiveCode;

public struct LiveCodeResult
{
    public string sourceCode;
    public string[] compilerErrors;
    public RunnerResult runnerResult;

    public bool HasSuccess
    {
        get
        {
            if (compilerErrors is not null)
            {
                return false;
            }

            if (runnerResult.status != RunnerResultStatus.OK)
            {
                return false;
            }

            return true;
        }
    }

    public bool HasException
    {
        get
        {
            if (compilerErrors is not null)
            {
                return false;
            }

            if (runnerResult.status != RunnerResultStatus.Exception)
            {
                return false;
            }

            return true;
        }
    }

    public override string ToString()
    {
        if (compilerErrors is not null)
        {
            return string.Join(Environment.NewLine, compilerErrors);
        }

        if (HasSuccess && runnerResult.returnValue is not null)
        {
            return runnerResult.returnValue.ToString();
        }

        var result = "[" + runnerResult.status + "]";

        if (HasException && runnerResult.returnValue is not null)
        {
            result += Environment.NewLine + runnerResult.returnValue;
        }

        return result;
    }
}
