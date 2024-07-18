using System.Reflection;
using System.Runtime.CompilerServices;

namespace Brandmauer.LiveCode;

public class Runner : IDisposable
{
    public float timeoutInSeconds = -1;

    public event EventHandler OnDispose;

    readonly AssemblyReference assemblyReference;

    public Runner(CompilerResult compilerResult) : this(compilerResult.rawAssembly) { }
    public Runner(byte[] rawAssembly) : this(AssemblyReference.Create(rawAssembly)) { }
    public Runner(AssemblyReference assemblyReference)
    {
        this.assemblyReference = assemblyReference;
    }

    public Task<RunnerResult> ExecuteAsync(params object[] args)
    {
        return ExecuteAsync(CancellationToken.None, args);
    }

    public async Task<RunnerResult> ExecuteAsync(CancellationToken cancellationToken, object[] args)
    {
        var result = new RunnerResult();

        var assembly = assemblyReference.Assembly;

        if (assembly is null)
        {
            return result.Status(RunnerResultStatus.AssemblyNotFound);
        }

        var type = assembly
            .GetTypes()
            .Where(x => x.IsAbstract && x.IsSealed)
            .FirstOrDefault();

        if (type is null)
        {
            return result.Status(RunnerResultStatus.ClassNotFound);
        }

        var method = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static
        ).FirstOrDefault();

        if (method is null)
        {
            return result.Status(RunnerResultStatus.MethodNotFound);
        }

        var parameters = new List<object>();

        foreach (var methodParameter in method.GetParameters())
        {
            var defaultValue = methodParameter.DefaultValue;

            if (defaultValue is DBNull)
            {
                var parameterType = methodParameter.ParameterType;
                defaultValue = parameterType.IsValueType
                    ? Activator.CreateInstance(parameterType)
                    : null;
            }

            parameters.Add(defaultValue);
        }

        for (int i = 0; i < args.Length && i < parameters.Count; i++)
        {
            parameters[i] = args[i];
        }

        Task<object> methodTask;

        if (method.GetCustomAttribute<AsyncStateMachineAttribute>() is null)
        {
            methodTask = Task.Run(() =>
            {
                // invoke method normally
                try
                {
                    return method.Invoke(null, [.. parameters]);
                }
                catch (Exception ex)
                {
                    result.status = RunnerResultStatus.Exception;
                    return ex;
                }
            });
        }
        else
        {
            methodTask = Task.Run(() =>
            {
                // invoke method and wait for the result of an async task
                try
                {
                    dynamic awaitable = method.Invoke(null, [.. parameters]);
                    var resultProperty = (awaitable.GetType() as Type).GetProperty("Result");
                    return resultProperty.GetValue(awaitable);
                }
                catch (Exception ex)
                {
                    result.status = RunnerResultStatus.Exception;
                    return ex;
                }
            });
        }

        Task timeoutTask;

        if (timeoutInSeconds <= 0)
        {
            timeoutTask = GetInfinityTask(cancellationToken);
        }
        else
        {
            timeoutTask = Task.Delay((int) (timeoutInSeconds * 1000), cancellationToken);
        }

        var tasks = new List<Task> { methodTask, timeoutTask };

        var resultTask = await Task.WhenAny(tasks);

        if (resultTask.IsCanceled)
        {
            result.status = RunnerResultStatus.Canceled;
        }
        else if (resultTask == timeoutTask)
        {
            result.status = RunnerResultStatus.Timeout;
        }
        else if (resultTask == methodTask)
        {
            if (result.status != RunnerResultStatus.Exception)
            {
                result.status = RunnerResultStatus.OK;
            }

            if (method.ReturnType != typeof(void) && method.ReturnType != typeof(Task))
            {
                result.returnValue = methodTask.Result;
            }
        }

        foreach (var task in tasks)
        {
            try { task.Dispose(); } catch { }
        }

        return result;
    }

    static async Task GetInfinityTask(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    public void Dispose()
    {
        var reference = assemblyReference.Reference;

        if (reference is not null)
        {
            while (reference.IsAlive)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                GC.WaitForPendingFinalizers();
            }
        }

        OnDispose?.Invoke(this, EventArgs.Empty);
    }
}
