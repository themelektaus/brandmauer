#if LINUX
namespace Brandmauer;

using static FileUtils;

[Delay(0)]
[Interval(1)]
public class IntervalTask_UpdateBrandmauer : IntervalTask
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

    readonly WebApplication app;

    public IntervalTask_UpdateBrandmauer(WebApplication app)
    {
        this.app = app;
    }

    protected override Task OnStartAsync() => default;

    protected override Task OnBeforeFirstTickAsync() => default;

    protected override async Task OnTickAsync()
    {
        if (!Directory.Exists(UPDATE_FOLDER))
            return;

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

    protected override Task OnDisposeAsync() => default;
}
#endif
