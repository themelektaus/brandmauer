using System.Diagnostics;
using System.IO.Compression;
using System.Security.Principal;

var pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());

if (!pricipal.IsInRole(WindowsBuiltInRole.Administrator))
{
    Process.Start(new ProcessStartInfo
    {
        FileName = "Updater.exe",
        UseShellExecute = true,
        Verb = "runas",
    });
    return;
}

await Process.Start("cmd", "/c net stop Brandmauer").WaitForExitAsync();

try
{
    var processPath = Environment.ProcessPath;
    Environment.CurrentDirectory = Path.GetDirectoryName(processPath);
}
catch (Exception ex)
{
    Console.WriteLine(ex);
    goto Exit;
}

using (var httpClient = new HttpClient())
{
    try
    {
        using var stream = await httpClient.GetStreamAsync(
            "http://steinalt.online/download/brandmauer/windows.zip"
        );
        using var zip = new ZipArchive(stream);
        zip.ExtractToDirectory(".", true);
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}

Exit:
await Process.Start("cmd", "/c net start Brandmauer").WaitForExitAsync();
