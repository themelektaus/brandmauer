namespace Brandmauer.LiveCode;

public interface IAsyncCompiler
{
    public Task<CompilerResult> CompileAsync();
}
