namespace Brandmauer;

public static class IntervalTaskExtensionMethods
{
    static readonly List<IntervalTask> instances = new();

    public static void RunInBackground<T>(
        this WebApplication @this
    ) where T : IntervalTask
    {
        var constructor = typeof(T).GetConstructors().FirstOrDefault();
        var paramters = constructor.GetParameters();

        IntervalTask instance;
        if (paramters.Length > 0)
            instance = Activator.CreateInstance(typeof(T), new[] { @this })
                as IntervalTask;
        else
            instance = Activator.CreateInstance<T>();

        instances.Add(instance);
        instance.RunInBackground();
    }

    public static async Task DisposeAllIntervalTasksAsync(
        this WebApplication _
    )
    {
        foreach (var instance in instances)
            await instance.DisposeAsync();

        instances.Clear();
    }
}
