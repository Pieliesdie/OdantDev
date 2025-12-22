using System.IO.Compression;
using System.Net.Http;

namespace SharedOdantDev.Common;

/// <summary>
/// Example Usage: await DevHelpers.DownloadAndCopyFramework4_0And4_5();
/// </summary>
public class DevHelpers
{
    public static async Task DownloadAndCopyFramework4_0And4_5Async(ILogger? logger = null)
    {
        await DownloadAndCopyFramework4_0Async(logger);
        await DownloadAndCopyFramework4_5Async(logger);
    }

    public static async Task DownloadAndCopyFramework4_5Async(ILogger? logger = null)
    {
        await DownloadAndCopyFrameworkGenericAsync("net45", "v4.5", "1.0.2", logger);
    }

    public static async Task DownloadAndCopyFramework4_0Async(ILogger? logger = null)
    {
        await DownloadAndCopyFrameworkGenericAsync("net40", "v4.0", "1.0.2", logger);
    }

    private static async Task RemoveExistsFolderWithAdminRightsAsync(string path)
    {
        await InvokeCmdCommandAsync($"rmdir \"{path}\" /Q /S");
    }

    private static async Task CopyFolderWithAdminRightsAsync(string from, string to)
    {
        await InvokeCmdCommandAsync($"Xcopy \"{from}\" \"{to}\" /E /H /C /I /y");
    }
    public static async Task InvokeCmdCommandAsync(string command, string workingDirectory = "")
    {
        await Task.Run(() => InvokeCmdCommand(command, workingDirectory));
    }
    public static string InvokeCmdCommand(string command, string workingDirectory = "")
    {
        System.Diagnostics.ProcessStartInfo startInfo = new()
        {
            WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal,
            Verb = "runas",
            FileName = "cmd.exe",
            Arguments = $"/C {command}",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
        };
        var process = new System.Diagnostics.Process
        {
            StartInfo = startInfo
        };
        process.Start();
        process.WaitForExit();
        var ex = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(ex))
        {
            throw new Exception(ex);
        }    

        return process.StandardOutput.ReadToEnd();
    }

    public static async Task DownloadAndCopyFrameworkGenericAsync(string netVersion, string folder, string nugetVersion, ILogger logger = null)
    {
        logger?.LogInformation("Start loading .Net {Folder}", folder);
        var url = $"https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.{netVersion}/{nugetVersion}";
        var netFolder = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\" + folder;
        var netFolder2 = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\" + folder;

        var tempFolder = Path.GetTempPath() + Guid.NewGuid();
        var tempFileName = $"{tempFolder}\\{Guid.NewGuid()}.zip";
        var tempExtractFolder = $"{tempFolder}\\{folder}";
        var tempExtractNetFolder = $"{tempExtractFolder}\\build\\.NETFramework\\{folder}";
        try
        {
            Directory.CreateDirectory(tempFolder);
            await DownloadFileAsync(tempFileName, url);
            logger?.LogInformation("Copy to references folder");
            ZipFile.ExtractToDirectory(tempFileName, tempExtractFolder);

            if (Directory.Exists(netFolder))
            {
                await RemoveExistsFolderWithAdminRightsAsync(netFolder);
            }
            await CopyFolderWithAdminRightsAsync(tempExtractNetFolder, netFolder);

            if (Directory.Exists(netFolder2))
            {
                await RemoveExistsFolderWithAdminRightsAsync(netFolder2);
            }
            await CopyFolderWithAdminRightsAsync(tempExtractNetFolder, netFolder2);
            logger?.LogInformation(".Net {Folder} loaded", folder);
            logger?.LogInformation($"Please reopen Visual Studio");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
        }
    }

    private static string DateTimeToFileString(DateTime d)
    {
        return d.ToString("yyyy-dd-M--HH-mm-ss");
    }

    private static async Task DownloadFileAsync(string fileName, string url)
    {
        var uri = new Uri(url);
        using var client = new HttpClient();
        var response = await client.GetAsync(uri);
        using var fs = new FileStream(fileName, FileMode.CreateNew);
        await response.Content.CopyToAsync(fs);
    }

    public static string ClearDomainAndClassInPath(string path)
    {
        if (path.EndsWith("\\CLASS"))
        {
            path = path.Substring(0, path.Length - 6);
        }

        if (path.EndsWith("\\DOMAIN"))
        {
            path = path.Substring(0, path.Length - 7);
        }

        return path;
    }

    public static void SetAttributesNormal(DirectoryInfo dir)
    {
        foreach (var subDir in dir.GetDirectories())
        {
            SetAttributesNormal(subDir);
            subDir.Attributes = FileAttributes.Normal;
        }
        // Parallel.ForEach(dir.EnumerateFiles(), e => e.Attributes = FileAttributes.Normal);
        foreach (var file in dir.GetFiles())
        {
            file.Attributes = FileAttributes.Normal;
        }
    }
}
