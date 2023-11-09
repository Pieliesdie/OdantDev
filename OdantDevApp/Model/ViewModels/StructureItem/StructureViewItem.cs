using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using oda;

using OdantDev;
using OdantDev.Model;

using OdantDevApp.Model.ViewModels.Settings;

using odaServer;

using SharedOdanDev.OdaOverride;

using SharedOdantDev.Common;

namespace OdantDevApp.Model.ViewModels;

public partial class StructureViewItem<T> : ObservableObject where T : StructureItem
{
    private static readonly IEnumerable<StructureViewItem<T>> dummyList = new List<StructureViewItem<T>>() { new() { Name = "Loading...", Icon = PredefinedImages.LoadImage } };
    private readonly ILogger? logger;
    private readonly ConnectionModel? connection;
    private bool isLoaded;
    private readonly NativeMethods.OdaServerApi.OnUpdate_CALLBACK updateCallback;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
    private async void Updated(int type, IntPtr @params)
    {
        try
        {
            switch ((StructureItemEvent)type)
            {
                case StructureItemEvent.Update:
                case StructureItemEvent.Delete:
                    {
                        if (Parent != null)
                        {
                            await Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                        }
                        break;
                    }
                case StructureItemEvent.None:
                case StructureItemEvent.Create:
                    await RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                    break;
            }
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            logger?.Info(ex.Message);
        }
    }

    private ODAItem? RemoteItem => this.Item?.RemoteItem;
    public bool IsPinned => this is StructureViewItem<StructureItem> item && (connection?.PinnedItems?.Contains(item) ?? false);
    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule && IsLocal;
    public bool CanOpenModule => HasModule && IsLocal;
    public bool CanDownloadModule => Item is Class && HasModule && !IsLocal;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item is { IsDisposed: false };
    public ItemType ItemType { get; private set; }

    [ObservableProperty] private string name;

    [ObservableProperty] private T? item;

    [ObservableProperty] private StructureViewItem<T>? parent;

    [ObservableProperty] private ImageSource? icon;

    [ObservableProperty] private IEnumerable<StructureViewItem<T>>? children;

    [ObservableProperty] private bool isExpanded;

    async partial void OnIsExpandedChanged(bool value)
    {
        if (!value || isLoaded) return;
        isLoaded = true;
        if (Children is not null && Children != dummyList)
            return;
        if (Item != null)
            await SetChildrenAsync(Item, logger, connection);
    }
    public bool HasModule
    {
        get
        {
            if (Item is null)
                return false;
            switch (ItemType)
            {
                case ItemType.Class:
                    return (Item as Class)?.HasModule ?? false;
                case ItemType.Module:
                    {
                        if (Children == dummyList || Children is null)
                            Children = GetChildren(Item, this, logger, connection).ToArray();

                        return Children?.Any(x => x.HasModule) ?? false;
                    }
                default:
                    return false;
            }
        }
    }

    public bool HasChildren
    {
        get
        {
            if (Item is Host or null)
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
    public StructureViewItem() { }
    public StructureViewItem(T item, StructureViewItem<T>? parent = null, ILogger? logger = null, ConnectionModel? connection = null) : this()
    {
        this.logger = logger;
        this.connection = connection;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        updateCallback = Updated;
        NativeMethods.OdaServerApi.SetOnUpdate(Item.RemoteItem.GetIntPtr(), updateCallback);
        _ = SetExpanderAsync();
        _ = SetIconAsync();
    }

    private async Task SetExpanderAsync()
    {
        await Task.Run(() =>
        {
            if (HasChildren)
            {
                Children = dummyList;
            }
        });
    }

    private async Task SetIconAsync()
    {
        if (Item is not null)
        {
            Icon = await Item.GetImageSourceAsync();
        }
    }

    private async Task SetChildrenAsync(T item, ILogger? logger = null, ConnectionModel? connection = null)
    {
        try
        {
            Children = await Task.Run(() => GetChildren(item, this, logger, connection).ToArray())
                .WithTimeout(TimeSpan.FromSeconds(AddinSettings.Instance.StructureItemTimeout));
        }
        catch (TimeoutException)
        {
            logger?.Info($"Timeout when getting children for {this}");
        }
        catch
        {
            logger?.Info($"Unhandled exception for {this}");
            throw;
        }
        finally
        {
            Children = null;
            await SetExpanderAsync();
        }
    }

    private static IEnumerable<StructureViewItem<T>> GetChildren(T item, StructureViewItem<T>? parent = null, ILogger? logger = null, ConnectionModel? connection = null)
    {
        var items = item.FindConfigItems().OrderBy(x => x.Name).ToLookup(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution);

        IEnumerable<StructureViewItem<T>> children = items[true]
            .Select(child => new StructureViewItem<T>(child as T, parent, logger, connection))
            .OrderBy(x => x.Item?.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>();
        var workplaces = items[false].OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureViewItem<T>()
            {
                Name = "Workplaces",
                Icon = PredefinedImages.WorkplaceImage,
                Children = workplaces.Select(child => new StructureViewItem<T>(child as T, parent, logger, connection))
            };
            yield return workplace;
        }
        if (modules.Any())
        {
            var module = new StructureViewItem<T>()
            {
                Name = "Modules",
                Icon = PredefinedImages.ModuleImage,
                Children = modules.Select(child => new StructureViewItem<T>(child as T, parent, logger, connection))
            };
            yield return module;
        }

        //hide inner class in domain
        foreach (var child in children)
        {
            if (child?.RemoteItem?.Id == item.RemoteItem.Id)
            {
                NativeMethods.OdaServerApi.SetOnUpdate(child.RemoteItem.GetIntPtr(), parent.updateCallback);
                var rootItems = GetChildren(child.Item, parent, logger, connection);
                //create folders for categories
                if (item.ItemType == ItemType.Organization)
                {
                    rootItems = ApplyCategories(rootItems);
                }
                foreach (var rootItem in rootItems)
                {
                    yield return rootItem;
                }
                continue;
            }
            yield return child;
        }
    }

    private static IEnumerable<StructureViewItem<T>> ApplyCategories
        (IEnumerable<StructureViewItem<T>> structureItems, string subGroupParent = "")
    {
        var emptyGroupedChildren = structureItems.ToLookup(x => string.IsNullOrWhiteSpace(x.Item?.Category));

        var groups = emptyGroupedChildren[false]
            .GroupBy(x => x.Item?.Category.SubstringAfter(subGroupParent).SubstringBefore("/"))
            .OrderBy(x => x.Key);
        foreach (var group in groups)
        {
            var rootItems = group.ToLookup(x => group.Key == x.Item?.Category.SubstringAfter(subGroupParent));
            yield return new StructureViewItem<T>()
            {
                Name = group.Key,
                Icon = PredefinedImages.FolderImage,
                Children = ApplyCategories(rootItems[false], $"{group.Key}/").Concat(rootItems[true])
            };
        }

        foreach (var item in emptyGroupedChildren[true])
        {
            yield return item;
        }
    }

    public async Task RefreshAsync(bool force = false)
    {
        if (Item == null) { return; }

        Item.Reset();
        (Item as Class)?.ReloadClassFromServer();
        if (isLoaded || force)
        {
            await SetChildrenAsync(this.Item, logger, connection);
        }
        else
        {
            await SetExpanderAsync();
        }
        await SetIconAsync();
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        OnPropertyChanged(nameof(Item));
        OnPropertyChanged(nameof(HasModule));
    }
    public override string ToString() => Name;
}