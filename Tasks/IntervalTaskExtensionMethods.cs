namespace Brandmauer;

public static class IntervalTaskExtensionMethods
{
    static readonly List<IntervalTask> instances = new();

    public static void RunInBackground(this WebApplication @this, Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault();
        var paramters = constructor.GetParameters();

        var instance = (
            paramters.Length == 1
                ? Activator.CreateInstance(type, [@this])
                : Activator.CreateInstance(type)
            ) as IntervalTask;

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
