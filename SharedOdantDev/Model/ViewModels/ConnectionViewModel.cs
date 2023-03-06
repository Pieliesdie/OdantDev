using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.VisualBasic.FileIO;
using oda;
using OdantDev.Model;
using SharedOdantDev.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using GitLabApiClient.Models.Users.Responses;
using SharedOdantDev.Common;

namespace OdantDev;

[ObservableObject]
public partial class ConnectionModel 
{
    public static readonly string[] odaClientLibraries = { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
    public static readonly string[] odaServerLibraries = { "odaClient.dll", "fastxmlparser.dll", "ucrtbase.dll" };
    private readonly ILogger logger;

    [ObservableProperty]
    private List<StructureItemViewModel<StructureItem>> hosts;

    [ObservableProperty]
    private List<RepoBaseViewModel> _repos;

    [ObservableProperty]
    private List<DomainDeveloper> developers;

    public static List<IntPtr> ServerAssemblies { get; set; }

    public static List<Assembly> ClientAssemblies { get; set; }

    [ObservableProperty]
    private bool autoLogin;

    partial void OnAutoLoginChanged(bool value)
    {
        Connection.AutoLogin = value;
    }

    public Connection Connection { get; }
    public AddinSettings AddinSettings { get; }

    public ConnectionModel(Connection connection, AddinSettings addinSettings, ILogger logger = null)
    {
        this.Connection = connection;
        this.AddinSettings = addinSettings;
        this.logger = logger;
    }


    [HandleProcessCorruptedStateExceptions]
    public async Task<(bool Success, string Error)> LoadAsync()
    {
        Stopwatch stopWatch = null;
        try
        {
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            stopWatch = new Stopwatch();
            stopWatch.Start();
            Connection.CoreMode = CoreMode.AddIn;
            var connected = await Task.Run(() => Connection.Login());
            if (connected.Not())
                return (false, "Can't connect to oda");

            AutoLogin = Connection.AutoLogin;
            
            Hosts = await HostsListAsync();
            await InitReposAsync();
            Developers = await DevelopListAsync();
            if (Developers.Any() && Connection?.LocalHost?.Develope is { } developDomain)
            {
                Hosts = Hosts?.Prepend(new StructureItemViewModel<StructureItem>(developDomain, logger: logger))?.ToList();
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            logger?.Info($"Load time: {elapsedTime}");
            Common.DebugINI.Clear();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message ?? ex.ToString());
        }
        finally
        {
            stopWatch?.Stop();
        }
    }

    private async Task<List<DomainDeveloper>> DevelopListAsync()
    {
        return await Task.Run(async () =>
        {
            var developList = Developers = Connection?.LocalHost?.Develope?.Domains?.OfType<DomainDeveloper>()?.ToList();
            var retryCount = 5;
            while (retryCount-- > 0 && developList is null)
            {
                await Task.Delay(1000);
            }
            return developList ?? new List<DomainDeveloper>();
        });
    }

    public async Task InitReposAsync()
    {
        try
        {
            Repos = await ReposListAsync();
        }
        catch (Exception)
        {
        }
    }

    private async Task<List<RepoBaseViewModel>> ReposListAsync()
    {
        if (string.IsNullOrEmpty(AddinSettings.GitLabApiPath) || string.IsNullOrEmpty(AddinSettings.GitLabApiKey))
            return null;

        GitClient.CreateClient(AddinSettings.GitLabApiPath, AddinSettings.GitLabApiKey);

        var item = new RootItem(GitClient.Client.HostUrl);
        return await Task.Run(() =>
        {
            var list = new List<RepoBaseViewModel>
            {
                new RepoRootViewModel(item, true, true, logger: logger)
            };
            return list;
        });
    }

    private async Task<List<StructureItemViewModel<StructureItem>>> HostsListAsync()
    {
        return await Task.Run(async () =>
        {
            var retryCount = 10;
            while (retryCount-- > 0 && Connection?.LocalHost?.OnLine == false)
            {
                Connection?.LocalHost?.Reset();
                await Task.Delay(1000);
            }
            return Connection
                ?.Hosts
                ?.Sorted
                ?.OfType<Host>()
                ?.Select(host => new StructureItemViewModel<StructureItem>(host, logger: logger))
                ?.ToList();
        });
    }

    public async Task<(bool Success, string Error)> RefreshAsync()
    {
        try
        {
            Connection.ResetUser();
            Connection.Reset();
            return await LoadAsync();
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
    public static (bool Success, string Error) LoadOdaLibraries(DirectoryInfo odaFolder)
    {
        try
        {
            ServerAssemblies = Extension.LoadServerLibraries(odaFolder.FullName, Extension.Platform , odaServerLibraries);
            ClientAssemblies = Extension.LoadClientLibraries(odaFolder.FullName, odaClientLibraries);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public StructureItem CreateItemsFromFiles(StructureItem rootItem, DirectoryInfo rootDir)
    {
        ItemType itemType;
        DirectoryInfo searchDir;

        var domainDirs = rootDir.GetDirectories("DOMAIN");
        if (domainDirs.Length > 0)
        {                
            searchDir = domainDirs[0];
            itemType = ItemType.Domain;
        }
        else
        {
            searchDir = rootDir;
            itemType = ItemType.Class;
        }

        StructureItem root = CreateItemFromFiles(rootItem, searchDir, itemType, out bool isOclExists);

        if (!isOclExists)
        {
            try
            {
                string path = root.Dir.RemoteFolder.LoadFolder();
                path = DevHelpers.ClearDomainAndClassInPath(path);

                FileSystem.CopyDirectory(rootDir.FullName, System.IO.Path.Combine(path, rootDir.Name), UIOption.OnlyErrorDialogs);              
            }
            catch
            {
            }
        }
        else
        {
            foreach (DirectoryInfo dir in rootDir.GetDirectories("*"))
            {
                if (dir.Name is "DOMAIN" or "CLASS")
                    continue;

                CreateItemsFromFiles(root, dir);
            }

            foreach (FileInfo file in rootDir.GetFiles("*"))
            {
                string path = root.Dir.RemoteFolder.LoadFolder();
                path = DevHelpers.ClearDomainAndClassInPath(path);

                FileSystem.CopyFile(file.FullName, System.IO.Path.Combine(path, file.Name), UIOption.OnlyErrorDialogs);
            }
        }

        return root;
    }

    private StructureItem CreateItemFromFiles(StructureItem root, DirectoryInfo dir, ItemType itemType, out bool success)
    {
        success = false;
        StructureItem tempRoot = root;

        DirectoryInfo[] domainClassDirs = dir.GetDirectories("CLASS");
        if (domainClassDirs.Length > 0)
        {
            
            DirectoryInfo classDir = domainClassDirs[0];
            var oclFiles = classDir.GetFiles("class.ocl");
            if (oclFiles.Length > 0)
            {
                success = true;

                FileInfo oclFile = oclFiles[0];

                tempRoot = CreateItem(tempRoot, oclFile.FullName, itemType);
                CopyFilesToStructureItem(tempRoot, classDir);
            }
        }

        return tempRoot;
    }

    private static void CopyFilesToStructureItem(StructureItem item, DirectoryInfo classDir)
    {
        foreach (DirectoryInfo dir in classDir.EnumerateDirectories())
        {
            item.Dir.SaveFile(dir.FullName);
        }
        foreach (FileInfo file in classDir.EnumerateFiles())
        {
            if (file.Name == "class.ocl")
                continue;

            item.Dir.SaveFile(file.FullName);
        }
    }

    private StructureItem CreateItem(Item rootItem, string oclFilePath, ItemType itemType)
    {
        string xml = System.IO.File.ReadAllText(oclFilePath);

        StructureItem resultItem;
        switch (itemType)
        {
            case ItemType.Class:
                string newCid = rootItem.Command("create_class", xml);
                resultItem = rootItem.FindClass(newCid);                    
                break;
                
            case ItemType.Domain:
                string newDomainId = rootItem.Command("create_domain", xml);
                resultItem = rootItem.FindDomain(newDomainId);
                break; 
            default:
                return null;
        }

        System.IO.File.Copy(oclFilePath, Path.Combine(resultItem.Dir.RemoteFolder.LoadFolder(), "class.ocl"), true);

        return resultItem;
    }
}
