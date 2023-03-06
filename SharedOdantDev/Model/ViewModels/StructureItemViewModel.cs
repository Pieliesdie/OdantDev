using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using oda;

using odaCore;

using odaServer;

using SharedOdanDev.OdaOverride;

using SharedOdantDev.Common;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OdantDev.Model;

[ObservableObject]
public partial class StructureItemViewModel<T> where T : StructureItem
{
    static IEnumerable<StructureItemViewModel<T>> dummyList = new List<StructureItemViewModel<T>>() { new StructureItemViewModel<T>() { Category = "Loading...", Icon = Images.GetImage(Images.GetImageIndex(Icons.Clock)).ConvertToBitmapImage() } };
    ILogger logger;
    bool isLoaded;

    delegate void Update(int Type, string Params);
    event Update OnUpdate;
    ServerApi.OnUpdate_CALLBACK Update_CALLBACK;
    void Updated(int type, IntPtr Params)
    {
        if (type < 6)
        {
            RefreshCommand.Execute(this);
        }
        OnUpdate?.Invoke(type, Params == IntPtr.Zero ? String.Empty : Marshal.PtrToStringUni(Params));
    }

    ODAItem RemoteItem => this.Item?.RemoteItem;
    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public string Name => $"{RemoteItem?.Label ?? RemoteItem?.Name ?? Category}";
    public bool CanCreateModule => Item is Class && !HasModule;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item != null;
    public ItemType ItemType { get; private set; }

    [ObservableProperty]
    T item;

    [ObservableProperty]
    T parent;

    [ObservableProperty]
    string category;

    [ObservableProperty]
    ImageSource icon;

    [ObservableProperty]
    IEnumerable<StructureItemViewModel<T>> children;

    [ObservableProperty]
    bool isExpanded;
    async partial void OnIsExpandedChanged(bool value)
    {
        if (value && !isLoaded)
        {
            isLoaded = true;
            if (Children is null || Children == dummyList)
            {
                await SetChildrenAsync(Item, logger);
            }        
        }
    }
    public bool HasModule
    {
        get
        {
            switch (ItemType)
            {
                case ItemType.Class:
                    return (Item as Class).HasModule;
                case ItemType.Module:
                    {
                        if (Children == dummyList || Children is null)
                            Children = GetChildren(Item, logger);

                        return Children?.Any(x => x.HasModule) ?? false;
                    }
            }
            return false;
        }
    }
    public bool HasChildren
    {
        get
        {
            if (Item is Host || Item is null)
            {
                return true;
            }
            var hasChildren = Item.RemoteItem.HasChilds;
            if (!hasChildren && ItemType != ItemType.Class)
            {
                hasChildren = Item.Class?.RemoteItem?.HasChilds ?? false;
            }
            return hasChildren;
        }
    }
    public StructureItemViewModel() { }
    public StructureItemViewModel(T item, T parent = null, ILogger logger = null) : this()
    {
        this.logger = logger;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Category = ItemType.ToString();
        Update_CALLBACK = Updated;
        GC.SuppressFinalize(Update_CALLBACK);
        ServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), Update_CALLBACK);
        _ = SetExpanderAsync();
        _ = SetIconAsync();
    }
    ~StructureItemViewModel()
    {
        if (item?.RemoteItem != null)
        {
            ServerApi._SetOnUpdate(item.RemoteItem.GetIntPtr(), null);
        }
        if (Update_CALLBACK != null)
        {
            GC.ReRegisterForFinalize(Update_CALLBACK);
        }
    }
    async Task SetExpanderAsync()
    {
        await Task.Run(() =>
        {
            if (HasChildren)
            {
                Children = dummyList;
            }
        });
    }
    async Task SetIconAsync()
    {
        if (Item is not null)
        {
            Icon = await Item.GetImageSource();
        }
    }
    async Task SetChildrenAsync(T item, ILogger logger = null)
    {
        var task = Task.Run(() => GetChildren(item, logger).ToList());
        if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))))
        {
            Children = await task;
        }
        else
        {
            logger?.Info($"Timeout when getting children for {item}");
            Children = null;
        }
    }
    static IEnumerable<StructureItemViewModel<T>> GetChildren(T item, ILogger logger = null)
    {
        var items = ServerApi.FindConfigItems(item);
        var children = items
            .Where(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution)
            .Select(child => new StructureItemViewModel<T>(child as T, item, logger))
            .OrderBy(x => x.Item.SortIndex)
            .ThenBy(x => x.Name);

        switch (item.ItemType)
        {
            case ItemType.Class:
            case ItemType.Module:
            case ItemType.Solution:
            case ItemType.Host:
                {
                    //TODO: refactor me
                    foreach (var child in children)
                    {
                        if (child.RemoteItem.Id == item.RemoteItem.Id)
                        {
                            var rootItems = GetChildren(child.Item as T, logger);
                            foreach (var rootItem in rootItems)
                            {
                                yield return rootItem;
                            }
                            continue;
                        }
                        yield return child;
                    }
                    yield break;
                }
        };
        var modules = items.OfType<DomainModule>();
        var workplaces = items.OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureItemViewModel<T>()
            {
                Category = "Workplaces",
                Icon = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage(),
                children = workplaces.Select(child => new StructureItemViewModel<T>(child as T, item, logger))
            };
            yield return workplace;
        }
        if (modules.Any())
        {
            var module = new StructureItemViewModel<T>()
            {
                Category = "Modules",
                Icon = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage(),
                children = modules.Select(child => new StructureItemViewModel<T>(child as T, item, logger))
            };
            yield return module;
        }
        //TODO: refactor me
        foreach (var child in children)
        {
            if(child.RemoteItem.Id == item.RemoteItem.Id)
            {
                var rootItems = GetChildren(child.Item as T, logger);
                foreach(var rootItem in rootItems)
                {
                    yield return rootItem;
                }
                continue;
            }
            yield return child;
        }
    }

    [RelayCommand]
    public async Task Refresh()
    {
        if (Item == null) { return; }
        Item.Reset();
        (Item as Class)?.ReloadClassFromServer();
        if (isLoaded)
        {
            await SetChildrenAsync(this.Item, logger);
        }
        await SetIconAsync();
        OnPropertyChanged("Name");
        OnPropertyChanged("Item");
        OnPropertyChanged("Icon");
    }

    [RelayCommand]
    public void Info()
    {
        if (Item == null) { return; }
        Clipboard.Clear();
        Clipboard.SetText(Item.FullId);
        logger?.Info($"FullId copied to clipboard!");
    }

    [RelayCommand]
    public void OpenDir()
    {
        DirectoryInfo dirPath = Item.Dir.ServerToFolder();
        if (dirPath is not { Exists: true })
            return;

        Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath.FullName));
    }

    public override string ToString() => Name;
}