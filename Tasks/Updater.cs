#if RELEASE
namespace Brandmauer;

using static FileUtils;

public class Updater : ConditionalIntervalTask
{
    readonly string sourceFile;
    readonly string sourceFile_libsass;
    readonly string sourceFolder;
    readonly string sourceFolder_wwwroot;

    readonly string targetFile;
    readonly string targetFile_libsass;
    readonly string targetFolder;
    readonly string targetFolder_wwwroot;

    readonly WebApplication app;

    public Updater(WebApplication app)
    {
        this.app = app;

        var processFile = new FileInfo(Environment.ProcessPath);

        sourceFile = Path.GetFullPath(Path.Combine("Update", processFile.Name));
        sourceFile_libsass = Path.GetFullPath(Path.Combine("Update", "libsass.so"));
        sourceFolder = Path.GetFullPath("Update");
        sourceFolder_wwwroot = Path.Combine(sourceFolder, "wwwroot");

        targetFile = processFile.FullName;
        targetFile_libsass = Path.Combine(processFile.Directory.FullName, "libsass.so");
        targetFolder = processFile.Directory.FullName;
        targetFolder_wwwroot = Path.Combine(targetFolder, "wwwroot");
    }

    protected override TimeSpan Delay => TimeSpan.Zero;
    protected override TimeSpan Interval => TimeSpan.FromSeconds(1);

    protected override bool ShouldTrigger()
    {
        return Directory.Exists(sourceFolder);
    }

    protected override async Task OnTriggerAsync()
    {
        try
        {
            MoveFolder(targetFolder_wwwroot, $"{targetFolder_wwwroot}.bak");
            CopyFolder(sourceFolder_wwwroot, targetFolder_wwwroot);

            MoveFile(targetFile, $"{targetFile}.bak");
            CopyFile(sourceFile, targetFile);

            MoveFile(targetFile_libsass, $"{targetFile_libsass}.bak");
            CopyFile(sourceFile_libsass, targetFile_libsass);

            DeleteFolder($"{targetFolder_wwwroot}.bak");
            DeleteFile($"{targetFile}.bak");
            DeleteFile($"{targetFile_libsass}.bak");

            await app.StopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            try
            {
                MoveFolder($"{targetFolder_wwwroot}.bak", targetFolder_wwwroot);
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex);
            }

            try
            {
                MoveFile($"{targetFile}.bak", targetFile);
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex);
            }

            try
            {
                MoveFile($"{targetFile_libsass}.bak", targetFile_libsass);
            }
            catch (Exception _ex)
            {
                Console.WriteLine(_ex);
            }
        }
        finally
        {
            DeleteFolder(sourceFolder);
        }
    }
}
#endif