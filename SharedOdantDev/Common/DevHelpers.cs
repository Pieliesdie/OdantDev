﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using OdantDev.Model;
using System.Security.Principal;
using System.Security.Permissions;
using System.Diagnostics;
using Microsoft.VisualStudio.Threading;

namespace SharedOdanDev.Common;

/// <summary>
/// Example Usage: await DevHelpers.DownloadAndCopyFramework4_0And4_5();
/// </summary>
public class DevHelpers
{
    public static async Task DownloadAndCopyFramework4_0And4_5Async(ILogger logger = null)
    {
        await DownloadAndCopyFramework4_0Async(logger);
        await DownloadAndCopyFramework4_5Async(logger);
    }

    public static async Task DownloadAndCopyFramework4_5Async(ILogger logger = null)
    {
        logger?.Info("Start loading .Net 4.5");
        await DownloadAndCopyFrameworkGenericAsync("net45", "v4.5", "1.0.2", logger);
        logger?.Info(".Net 4.5 loaded");
    }

    public static async Task DownloadAndCopyFramework4_0Async(ILogger logger = null)
    {
        logger?.Info("Start loading .Net 4.0");
        await DownloadAndCopyFrameworkGenericAsync("net40", "v4.0", "1.0.2", logger);
        logger?.Info(".Net 4.0 loaded");
    }

    private static async Task RemoveExistsFolderWithAdminRights(string path)
    {
        await InvokeCmdCommand($"rmdir \"{path}\" /Q /S");
    }

    private static async Task CopyFolderWithAdminRights(string from, string to)
    {
        await InvokeCmdCommand($"Xcopy \"{from}\" \"{to}\" /E /H /C /I /y");
    }

    private static async Task InvokeCmdCommand(string command)
    {
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        startInfo.Verb = "runas";
        startInfo.FileName = "cmd.exe";
        startInfo.Arguments = $"/C {command}";
        process.StartInfo = startInfo;
        process.Start();
        await process.WaitForExitAsync();
    }
    public static async Task DownloadAndCopyFrameworkGenericAsync(string netVersion, string folder, string nugetVersion, ILogger logger = null)
    {
        var url = $"https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.{netVersion}/{nugetVersion}";
        var netFolder = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\" + folder;
        var netFolder2 = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\" + folder;

        var tempFolder = Path.GetTempPath() + Guid.NewGuid().ToString();
        string tempFileName = $"{tempFolder}\\{Guid.NewGuid()}.zip";
        string tempExtractFolder = $"{tempFolder}\\{folder}";
        string tempExtractNetFolder = $"{tempExtractFolder}\\build\\.NETFramework\\{folder}";
        try
        {
            Directory.CreateDirectory(tempFolder);
            await DownloadFileAsync(tempFileName, url);
            logger?.Info("Copy to references folder");
            ZipFile.ExtractToDirectory(tempFileName, tempExtractFolder);

            if (Directory.Exists(netFolder))
            {
                await RemoveExistsFolderWithAdminRights(netFolder);
            }
            await CopyFolderWithAdminRights(tempExtractNetFolder, netFolder);

            if (Directory.Exists(netFolder2))
            {
                await RemoveExistsFolderWithAdminRights(netFolder2);
            }
            await CopyFolderWithAdminRights(tempExtractNetFolder, netFolder2);
        }
        catch (Exception ex)
        {
            throw;
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
        using HttpClient client = new HttpClient();
        var response = await client.GetAsync(uri);
        using var fs = new FileStream(fileName, FileMode.CreateNew);
        await response.Content.CopyToAsync(fs);
    }
}
