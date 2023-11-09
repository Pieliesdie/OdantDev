﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using Microsoft.VisualBasic.FileIO;

using oda;

using OdantDev;
using OdantDev.Model;

using OdantDevApp.Common;
using OdantDevApp.Model.Git;
using OdantDevApp.Model.Git.GitItems;
using OdantDevApp.Model.ViewModels.Settings;

using SharedOdantDev.Common;

using RepoBase = OdantDevApp.Model.Git.RepoBase;

namespace OdantDevApp.Model.ViewModels;

public partial class ConnectionModel : ObservableObject, IDisposable
{
    public static List<IntPtr> ServerAssemblies { get; set; }
    public static List<Assembly> ClientAssemblies { get; set; }
    public static string[] OdaClientLibraries { get; } = { "odaLib.dll", "odaShare.dll", "odaXML.dll", "odaCore.dll" };
    private static string[] OdaServerLibraries { get; } = { "odaClient.dll", "fastxmlparser.dll", "ucrtbase.dll" };

    private readonly ILogger logger;
    private Connection? Connection { get; }

    [ObservableProperty] Domain develope;

    [ObservableProperty] Host? localhost;

    [ObservableProperty] AsyncObservableCollection<StructureViewItem<StructureItem>> hosts;

    [ObservableProperty] AsyncObservableCollection<StructureViewItem<StructureItem>> pinnedItems;

    [ObservableProperty] 
    ConcatenatedCollection<AsyncObservableCollection<StructureViewItem<StructureItem>>, StructureViewItem<StructureItem>> items;

    [ObservableProperty] List<RepoBase> repos;

    [ObservableProperty] List<DomainDeveloper>? developers;

    [ObservableProperty] bool autoLogin;

    partial void OnAutoLoginChanged(bool value)
    {
        Connection.AutoLogin = value;
    }
    public AddinSettings AddinSettings { get; }

    public ConnectionModel(Connection connection, AddinSettings addinSettings, ILogger? logger = null)
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
            stopWatch = new Stopwatch();
            stopWatch.Start();

            Connection.CoreMode = CoreMode.AddIn;
            var connected = await Task.Run(() => Connection.Login());
            if (connected.Not())
                return (false, "Can't connect to oda");

            AutoLogin = Connection.AutoLogin;
            Hosts = new AsyncObservableCollection<StructureViewItem<StructureItem>>(await HostsListAsync());
            PinnedItems = new AsyncObservableCollection<StructureViewItem<StructureItem>>(await PinnedListAsync());
            Developers = await DevelopListAsync();

            AddinSettings.SelectedDevelopeDomain ??= Developers?.FirstOrDefault()?.FullId;

            Items = new(PinnedItems, Hosts);

            oda.OdaOverride.INI.DebugINI.Clear();
            await oda.OdaOverride.INI.DebugINI.SaveAsync();

            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            logger?.Info($"Load time: {elapsedTime}");

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
    private async Task<IEnumerable<StructureViewItem<StructureItem>>> PinnedListAsync()
    {
        return await Task.Run(() =>
        {
            return AddinSettings
            .PinnedItems
            .Select(x => StructureItemEx.FindItem(Connection, x))
            .Where(x => x != null)
            .Select(x => new StructureViewItem<StructureItem>(x, null, logger, this));
        });
    }

    private async Task<List<DomainDeveloper>> DevelopListAsync()
    {
        return await Task.Run(async () =>
        {
            Develope = StructureItemEx.FindDomain(Localhost, "D:DEVELOPE");

            var developList = await Retry.RetryAsync<List<DomainDeveloper>>(
                () => Develope?.FindDomains()?.OfType<DomainDeveloper>()?.ToList(),
                (e) => e != null,
                TimeSpan.FromMilliseconds(300),
                TimeSpan.FromSeconds(5)
            );
            Developers = developList;
            return developList;
        });
    }

    public async Task InitReposAsync()
    {
        try
        {
            Repos = await ReposListAsync();
        }
        catch
        {
        }
    }

    private async Task<List<RepoBase>?> ReposListAsync()
    {
        if (string.IsNullOrEmpty(AddinSettings.GitLabApiPath) || string.IsNullOrEmpty(AddinSettings.GitLabApiKey))
            return null;

        return await Task.Run<List<RepoBase>>((Func<Task<List<RepoBase>>>)(async () =>
        {
            await GitClient.CreateClientAsync(AddinSettings.GitLabApiPath, AddinSettings.GitLabApiKey);
            var uriHost = new Uri(GitClient.Client?.HostUrl).Host;
            var item = new RootItem(uriHost);
            var list = new List<Git.RepoBase>
            {
                new RepoRoot(item, true, logger: logger)
            };
            return list;
        }));
    }

    private async Task<IEnumerable<StructureViewItem<StructureItem>>> HostsListAsync()
    {
        return await Task.Run<IEnumerable<StructureViewItem<StructureItem>>>(async () =>
        {
            var hosts = Connection.FindHosts().ToList();

            var localHost = Localhost = hosts.FirstOrDefault(x => x.IsLocal);

            await Retry.RetryAsync(
                 () => { localHost?.Reset(); return localHost?.HostState ?? HostStates.Off; },
                 (e) => e == HostStates.On,
                 TimeSpan.FromMilliseconds(300),
                 TimeSpan.FromSeconds(10));

            return hosts
                .OrderBy(x => x.SortIndex)
                .ThenBy(x => x.Label)
                .Select(host => new StructureViewItem<StructureItem>(host, logger: logger, connection: this));
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
            ServerAssemblies = VsixExtension.LoadServerLibraries(odaFolder.FullName, VsixExtension.Platform, OdaServerLibraries);
            ClientAssemblies = VsixExtension.LoadClientLibraries(odaFolder.FullName, OdaClientLibraries);
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        var requestAssemblyName = new AssemblyName(args.Name);

        //return oda assemblies which was loaded before
        var tryResolveInClientAssemblies = ClientAssemblies.FirstOrDefault(x => new AssemblyName(x.FullName).Name == requestAssemblyName.Name);
        if (tryResolveInClientAssemblies != null)
            return tryResolveInClientAssemblies;

        //other cases
        return null;
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

        var root = CreateItemFromFiles(rootItem, searchDir, itemType, out bool isOclExists);

        if (!isOclExists)
        {
            try
            {
                var path = root.Dir.RemoteFolder.LoadFolder();
                path = DevHelpers.ClearDomainAndClassInPath(path);

                FileSystem.CopyDirectory(rootDir.FullName, System.IO.Path.Combine(path, rootDir.Name), UIOption.OnlyErrorDialogs);
            }
            catch
            {
            }
        }
        else
        {
            foreach (var dir in rootDir.GetDirectories("*"))
            {
                if (dir.Name is "DOMAIN" or "CLASS")
                    continue;

                CreateItemsFromFiles(root, dir);
            }

            foreach (var file in rootDir.GetFiles("*"))
            {
                var path = root.Dir.RemoteFolder.LoadFolder();
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

    private static StructureItem? CreateItem(Item rootItem, string oclFilePath, ItemType itemType)
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

    public void Dispose()
    {
        if (Connection != null)
        {
            Connection.Logout();
            Connection.ServerItem?.Dispose();
            Connection.Dispose();
        }
        ServerAssemblies.ForEach(x => NativeMethods.WinApi.FreeLibrary(x));
    }
}