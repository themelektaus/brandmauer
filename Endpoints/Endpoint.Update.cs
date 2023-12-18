#if !(DEBUG || WINDOWS)
using System.IO.Compression;
using _File = System.IO.File;
#endif

namespace Brandmauer;

#if !(DEBUG || WINDOWS)
using static FileUtils;
#endif

using static Results;

public static partial class Endpoint
{
    public static class Update
    {
#if (DEBUG || WINDOWS)
        public static IResult Check()
        {
            return Json(new
            {
                remoteVersion = "(Not available in debug/windows mode)",
                downloadedVersion = "(Not available in debug/windows mode)",
                installedVersion = Utils.GetAssemblyName().Version.ToString(),
            });
        }

        public static IResult Download()
        {
            return Unauthorized();
        }

        public static IResult Install()
        {
            return Unauthorized();
        }
#else
        const string REMOTE_REPOSITORY_URL
            = "https://steinalt.online/download/brandmauer";

        public static async Task<IResult> Check()
        {
            using var client = new HttpClient();

            var response = await client.GetAsync(
                $"{REMOTE_REPOSITORY_URL}/version.txt"
            );
            if (!response.IsSuccessStatusCode)
                return NotFound();

            var versionFile = Path.Combine("Download", "version.txt");
            var developerAppSettingsFile = Path.Combine(
                "Download",
                "appsettings.Development.json"
            );

            return Json(new
            {
                remoteVersion = await response.Content.ReadAsStringAsync(),
                downloadedVersion = _File.Exists(versionFile)
                    ? await _File.ReadAllTextAsync(versionFile)
                    : (
                        _File.Exists(developerAppSettingsFile)
                        ? "(Developer Version)"
                        : ""
                    ),
                installedVersion = Utils.GetAssemblyName().Version.ToString(),
            });
        }

        public static async Task<IResult> Download()
        {
            var versionFile = Path.Combine("Download", "version.txt");
            DeleteFile(versionFile);

            var dataZipFile = Path.Combine("Download", "data.zip");
            DeleteFile(dataZipFile);

            using var client = new HttpClient();
            var response = await client.GetAsync(
                $"{REMOTE_REPOSITORY_URL}/data.zip"
            );

            var dataZipContent = await response.Content.ReadAsByteArrayAsync();

            CreateFolder("Download");
            await _File.WriteAllBytesAsync(dataZipFile, dataZipContent);
            ZipFile.ExtractToDirectory(dataZipFile, "Download");

            DeleteFile(dataZipFile);

            response = await client.GetAsync(
                $"{REMOTE_REPOSITORY_URL}/version.txt"
            );
            var version = await response.Content.ReadAsStringAsync();
            await _File.WriteAllTextAsync(versionFile, version);

            return Ok();
        }

        public static IResult Install()
        {
            var developerAppSettingsFile = Path.Combine(
                "Download",
                "appsettings.Development.json"
            );

            if (!_File.Exists(developerAppSettingsFile))
            {
                if (!_File.Exists(Path.Combine("Download", "version.txt")))
                    return NoContent();
            }

            if (!MoveFolder("Download", "Update"))
                return NoContent();

            return Ok();
        }
#endif
    }
}
