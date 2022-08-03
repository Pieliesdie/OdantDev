using Microsoft.VisualStudio.RpcContracts.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using OdantDev.Model;

namespace SharedOdanDev.Common
{
    /// <summary>
    /// Example Usage: await DevHelpers.DownloadAndCopyFramework4_0And4_5();
    /// </summary>
    public class DevHelpers
    {
        public static async Task DownloadAndCopyFramework4_0And4_5Async(ILogger logger = null)
        {
            logger?.Info("Start loading .Net 4.0");
            await DownloadAndCopyFramework4_0Async(logger);
            logger?.Info(".Net 4.0 loaded");
            logger?.Info("Start loading .Net 4.5");
            await DownloadAndCopyFramework4_5Async(logger);
            logger?.Info(".Net 4.5 loaded");
        }

        public static async Task DownloadAndCopyFramework4_5Async(ILogger logger = null)
        {
            await DownloadAndCopyFrameworkGenericAsync("net45", "v4.5", "1.0.2", logger);
        }

        public static async Task DownloadAndCopyFramework4_0Async(ILogger logger = null)
        {
            await DownloadAndCopyFrameworkGenericAsync("net40", "v4.0", "1.0.2", logger);
        }

        public static async Task DownloadAndCopyFrameworkGenericAsync(string netVersion, string folder, string nugetVersion, ILogger logger = null)
        {
            var name = netVersion + "-" + DateTimeToFileString(DateTime.Now);
            string fileName = Path.GetTempPath() + Guid.NewGuid().ToString() + ".zip";
            var url = $"https://www.nuget.org/api/v2/package/Microsoft.NETFramework.ReferenceAssemblies.{netVersion}/{nugetVersion}";
            await DownloadFileAsync(fileName, url);
            ZipFile.ExtractToDirectory(fileName, name);
            var from = Path.Combine(name, @"build\.NETFramework\" + folder);
            var to = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\" + folder;
            logger?.Info("Copy to references folder");
            FileSystem.CopyDirectory(from, to, UIOption.AllDialogs);
            File.Delete(fileName);
        }

        private static string DateTimeToFileString(DateTime d)
        {
            return d.ToString("yyyy-dd-M--HH-mm-ss");
        }

        private static async Task DownloadFileAsync(string fileName, string url)
        {
            var uri = new Uri(url);
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(uri);
            using var fs = new FileStream(fileName,FileMode.CreateNew);
            await response.Content.CopyToAsync(fs);
        }
    }
}
