namespace Brandmauer.LiveCode;

public struct CompilerResult
{
    public struct Error
    {
        public string id;
        public string message;
        public int line;
    }

    public string sourceCode;
    public byte[] rawAssembly;
    public Error[] errors;

    public bool HasErrors()
    {
        if (errors is null)
        {
            return false;
        }

        if (errors.Length == 0)
        {
            return false;
        }

        return true;
    }
}
