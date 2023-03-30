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
public sealed partial class StructureItemViewModel<T>  where T : StructureItem
{
    static readonly IEnumerable<StructureItemViewModel<T>> dummyList = new List<StructureItemViewModel<T>>() { new StructureItemViewModel<T>() { Name = "Loading...", Icon = Images.GetImage(Images.GetImageIndex(Icons.Clock)).ConvertToBitmapImage() } };
    ILogger logger;
    bool isLoaded;

    delegate void Update(int Type, string Params);
    event Update OnUpdate;
    ServerApi.OnUpdate_CALLBACK Update_CALLBACK;
    void Updated(int type, IntPtr Params)
    {
        if (type == 2)
        {
            RefreshCommand.Execute(this);
            this.Parent?.RefreshCommand.Execute(this);
        }
        else if (type < 6)
        {
            RefreshCommand.Execute(this);
        }
        OnUpdate?.Invoke(type, Params == IntPtr.Zero ? String.Empty : Marshal.PtrToStringUni(Params));
    }

    ODAItem RemoteItem => this.Item?.RemoteItem;
    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item != null;
    public ItemType ItemType { get; private set; }

    [ObservableProperty]
    string name;

    [ObservableProperty]
    T item;

    [ObservableProperty]
    StructureItemViewModel<T> parent;

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
                            Children = GetChildren(Item, this, logger);

                        return Children?.OfType<StructureItemViewModel<T>>().Any(x => x.HasModule) ?? false;
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
    public StructureItemViewModel(T item, StructureItemViewModel<T> parent = null, ILogger logger = null) : this()
    {
        this.logger = logger;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        Update_CALLBACK = Updated;
        GC.SuppressFinalize(Update_CALLBACK);
        ServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), Update_CALLBACK);
        _ = SetExpanderAsync();
        _ = SetIconAsync();
    }
    ~StructureItemViewModel()
    {
        if (Item?.RemoteItem != null)
        {
            ServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), null);
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
        var task = Task.Run(() => GetChildren(item, this, logger).ToImmutableList());
        if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10))))
        {
            Children = await task;
        }
        else
        {
            logger?.Info($"Timeout when getting children for {this}");
            Children = null;
        }
    }

    IEnumerable<StructureItemViewModel<T>> GetChildren(T item, StructureItemViewModel<T> parent = null, ILogger logger = null)
    {
        var items = ServerApi.FindConfigItems(item)
            .ToLookup(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution);

        IEnumerable<StructureItemViewModel<T>> children = items[true]
            .Select(child => new StructureItemViewModel<T>(child as T, parent, logger))
            .OrderBy(x => x.Item.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>();
        var workplaces = items[false].OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureItemViewModel<T>()
            {
                Name = "Workplaces",
                Icon = Images.GetImage(Images.GetImageIndex(Icons.UserRole)).ConvertToBitmapImage(),
                Children = workplaces.Select(child => new StructureItemViewModel<T>(child as T, parent, logger))
            };
            yield return workplace;
        }
        if (modules.Any())
        {
            var module = new StructureItemViewModel<T>()
            {
                Name = "Modules",
                Icon = Images.GetImage(Images.GetImageIndex(Icons.MagentaFolder)).ConvertToBitmapImage(),
                Children = modules.Select(child => new StructureItemViewModel<T>(child as T, parent, logger))
            };
            yield return module;
        }

        //create folders for categories
        if (item.ItemType == ItemType.Organization)
        {
            var emptyGroupedChildren = children.ToLookup(x => string.IsNullOrWhiteSpace(x.Item.Category));

            foreach (var groupedItem in ApplyCategories(emptyGroupedChildren[false]))
                yield return groupedItem;
            children = emptyGroupedChildren[true];
        }

        //hide inner class in domain
        foreach (var child in children)
        {
            if (child.RemoteItem.Id == item.RemoteItem.Id)
            {
                var rootItems = GetChildren(child.Item as T, parent, logger);
                foreach (var rootItem in rootItems)
                {
                    yield return rootItem;
                }
                continue;
            }
            yield return child;
        }
    }

    IEnumerable<StructureItemViewModel<T>> ApplyCategories(IEnumerable<StructureItemViewModel<T>> structureItems, string subGroupParent = "")
    {
        var groups = structureItems
            .GroupBy(x => x.Item.Category.SubstringAfter(subGroupParent).SubstringBefore("/"))
            .OrderBy(x => x.Key);
        foreach (var group in groups)
        {
            var rootItems = group.ToLookup(x => group.Key == x.Item.Category.SubstringAfter(subGroupParent));
            yield return new StructureItemViewModel<T>()
            {
                Name = group.Key,
                Icon = Images.GetImage(Images.GetImageIndex(Icons.Folder)).ConvertToBitmapImage(),
                Children = ApplyCategories(rootItems[false], $"{group.Key}/").Concat(rootItems[true])
            };
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
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        OnPropertyChanged("Item");
        OnPropertyChanged("HasModule");
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
        DirectoryInfo dirPath = Item.Dir?.ServerToFolder();
        if (dirPath is not { Exists: true })
            return;

        Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath.FullName));
    }

    public override string ToString() => Name;
}