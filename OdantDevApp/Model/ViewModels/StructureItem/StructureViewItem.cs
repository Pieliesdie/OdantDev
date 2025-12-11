using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using MoreLinq;
using oda;
using OdantDev;
using OdantDev.Model;
using OdantDevApp.Model.Git;
using OdantDevApp.Model.ViewModels.Settings;
using odaServer;
using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.ViewModels;

public partial class StructureViewItem<T> : ObservableObject where T : StructureItem
{
    private static readonly IEnumerable<StructureViewItem<T>> dummyList = new List<StructureViewItem<T>>
        { new() { Name = "Loading...", Icon = PredefinedImages.LoadImage } };

    private readonly ILogger? logger;
    private readonly ConnectionModel? connection;
    private bool isLoaded;
    private readonly NativeMethods.OdaServerApi.OnUpdate_CALLBACK updateCallback;

    private async void Updated(int type, IntPtr @params)
    {
        await Task.Delay(1000); // одант не умеет так быстро обновляться
        try
        {
            switch ((StructureItemEvent)type)
            {
                case StructureItemEvent.Update:
                    if (Parent != null)
                    {
                        await Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(30));
                    }

                    break;
                case StructureItemEvent.Delete:
                {
                    if (Parent != null)
                    {
                        Parent.Children = Parent.Children?.Except([this]);
                        await Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(30));
                    }

                    break;
                }
                case StructureItemEvent.None:
                case StructureItemEvent.Create:
                    await RefreshAsync().WithTimeout(TimeSpan.FromSeconds(30));
                    break;
            }
        }
        catch (TimeoutException)
        {
        }
        catch (Exception ex)
        {
            logger?.Info(ex.Message);
        }
    }

    private ODAItem? RemoteItem => Item?.RemoteItem;

    public bool IsPinned => this is StructureViewItem<StructureItem> item &&
                            (connection?.PinnedItems?.Contains(item) ?? false);

    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule && IsLocal;
    public bool CanCreateDeveloper => Item?.ItemType == ItemType.DevPart && IsLocal;
    public bool CanOpenModule => HasModule && IsLocal;
    public bool CanDownloadModule => Item is Class && HasModule && !IsLocal;
    public bool CanCreateRepo => GitClient.Client?.HostUrl != null;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item is { IsDisposed: false };
    public ItemType ItemType { get; }

    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial T? Item { get; set; }

    [ObservableProperty] public partial StructureViewItem<T>? Parent { get; set; }

    [ObservableProperty] public partial ImageSource? Icon { get; set; }

    [ObservableProperty] public partial IEnumerable<StructureViewItem<T>>? Children { get; set; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    async partial void OnIsExpandedChanged(bool value)
    {
        if (!value || isLoaded) return;
        isLoaded = true;
        if (Children is not null && !Children.Equals(dummyList))
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
                    if (Children is null || Children.Equals(dummyList))
                    {
                        Children = GetChildren(Item, this, logger, connection).ToArray();
                    }

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

    public StructureViewItem(T item, StructureViewItem<T>? parent = null, ILogger? logger = null,
        ConnectionModel? connection = null) : this()
    {
        this.logger = logger;
        this.connection = connection;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        updateCallback = Updated;
        if (Item.RemoteItem is not null)
        {
            NativeMethods.OdaServerApi.SetOnUpdate(Item.RemoteItem.GetIntPtr(), updateCallback);
        }

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
            Children = await Task
                .Run(() => GetDistinctChildren(item, this, logger, connection).ToArray())
                .WithTimeout(TimeSpan.FromSeconds(AddinSettings.Instance.StructureItemTimeout));
        }
        catch (TimeoutException)
        {
            logger?.Info($"Timeout when getting children for {this}");
            Children = null;
            await SetExpanderAsync();
        }
        catch
        {
            logger?.Info($"Unhandled exception for {this}");
            Children = null;
            throw;
        }
    }

    private static IEnumerable<StructureViewItem<T>> GetDistinctChildren(T item, StructureViewItem<T>? parent = null,
        ILogger? logger = null, ConnectionModel? connection = null)
    {
        return GetChildren(item, parent, logger, connection)
            .ToArray()
            .GroupBy(x => x.Item?.FullId ?? x.Name)
            .Select(x => x.Last());
    }

    private static IEnumerable<StructureViewItem<T>> GetChildren(T item, StructureViewItem<T>? parent = null,
        ILogger? logger = null, ConnectionModel? connection = null)
    {
        var items = item
            .FindConfigItems()
            .OrderBy(x => x.Name)
            .ToLookup(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution);

        var children = items[true]
            .OfType<T>()
            .Select(child => new StructureViewItem<T>(child, parent, logger, connection))
            .OrderBy(x => x.Item?.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>();
        var workplaces = items[false].OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureViewItem<T>
            {
                Name = "Workplaces",
                Icon = PredefinedImages.WorkplaceImage,
                Children = workplaces.OfType<T>().Select(child => new StructureViewItem<T>(child, parent, logger, connection))
            };
            yield return workplace;
        }

        if (modules.Any())
        {
            var module = new StructureViewItem<T>
            {
                Name = "Modules",
                Icon = PredefinedImages.ModuleImage,
                Children = modules.OfType<T>().Select(child => new StructureViewItem<T>(child, parent, logger, connection))
            };
            yield return module;
        }

        //hide inner class in domain
        foreach (var child in children)
        {
            if (child.RemoteItem?.Id == item.RemoteItem.Id)
            {
                var childPtr = child.RemoteItem?.GetIntPtr();

                if (childPtr is null || parent is null || child.Item is null)
                {
                    continue;
                }

                NativeMethods.OdaServerApi.SetOnUpdate(childPtr.Value, parent.updateCallback);
                var rootItems = GetDistinctChildren(child.Item, parent, logger, connection);
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
            yield return new StructureViewItem<T>
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
        if (Item == null)
        {
            return;
        }

        try
        {
            Item.Reset();
        }
        catch (Exception e)
        {
            logger?.Info(e.ToString());
        }

        try
        {
            (Item as Class)?.ReloadClassFromServer();
        }
        catch (Exception e)
        {
            logger?.Info(e.ToString());
        }

        if (isLoaded || force)
        {
            await SetChildrenAsync(Item, logger, connection);
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