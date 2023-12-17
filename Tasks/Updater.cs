#if LINUX
namespace Brandmauer;

using static FileUtils;

public class Updater : ConditionalIntervalTask
{
    const string UPDATE_FOLDER = "Update";

    static readonly string[] folders = [
        "wwwroot"
    ];

    static readonly FileInfo processFile = new(Environment.ProcessPath);

    static readonly string[] files = [
        processFile.Name,
        $"{processFile.Name}.pdb",
        "libsass.so"
    ];

    static string GetSourcePath(string name)
        => Path.GetFullPath(Path.Combine(UPDATE_FOLDER, name));

    static string GetTargetPath(string name)
        => Path.GetFullPath(name);

    protected override TimeSpan Delay => TimeSpan.Zero;
    protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

    readonly WebApplication app;

    public Updater(WebApplication app)
    {
        this.app = app;
    }

    protected override Task OnStartAsync()
    {
        return Task.CompletedTask;
    }

    protected override Task OnBeforeFirstTickAsync()
    {
        return Task.CompletedTask;
    }

    protected override bool ShouldTrigger()
    {
        return Directory.Exists(UPDATE_FOLDER);
    }

    protected override async Task OnTriggerAsync()
    {
        await Task.Delay(1000);

        try
        {
            foreach (var folder in folders)
            {
                var target = GetTargetPath(folder);
                MoveFolder(target, $"{target}.bak");
                CopyFolder(GetSourcePath(folder), target);
            }

            foreach (var file in files)
            {
                var target = GetTargetPath(file);
                MoveFile(target, $"{target}.bak");
                CopyFile(GetSourcePath(file), target);
            }

            foreach (var folder in folders)
                DeleteFolder($"{GetTargetPath(folder)}.bak");

            foreach (var file in files)
                DeleteFile($"{GetTargetPath(file)}.bak");

            DeleteFolder(UPDATE_FOLDER);

            await app.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            foreach (var folder in folders)
            {
                try
                {
                    var target = GetTargetPath(folder);
                    MoveFolder($"{target}.bak", target);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine(_ex);
                }
            }

            foreach (var file in files)
            {
                try
                {
                    var target = GetTargetPath(file);
                    MoveFile($"{target}.bak", target);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine(_ex);
                }
            }
        }
        finally
        {
            try
            {
                foreach (var folder in folders)
                    DeleteFolder(GetSourcePath(folder));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    protected override Task OnDisposeAsync()
    {
        return Task.CompletedTask;
    }
}
#endif