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

using odaServer;

using SharedOdanDev.OdaOverride;

using SharedOdantDev.Common;

namespace OdantDevApp.Model.ViewModels;
public partial class StructureItemViewModel<T> : ObservableObject where T : StructureItem
{
    private static readonly IEnumerable<StructureItemViewModel<T>> dummyList = new List<StructureItemViewModel<T>>() { new() { Name = "Loading...", Icon = PredefinedImages.LoadImage } };
    private readonly ILogger? logger;
    private readonly ConnectionModel? connection;
    private bool isLoaded;
    private readonly NativeMethods.OdaServerApi.OnUpdate_CALLBACK updateCallback;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
    private async void Updated(int type, IntPtr @params)
    {
        try
        {
            switch (type)
            {
                case 3:
                    {
                        if (Parent != null)
                        {
                            await Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                        }

                        break;
                    }
                case 2:
                    {
                        await this.RefreshAsync();
                        if (Parent != null)
                        {
                            await Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                        }

                        break;
                    }
                case < 6:
                    await RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                    break;
            }

            OnUpdate?.Invoke(type, @params == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni(@params));
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            logger?.Info(ex.Message);
        }
    }

    private ODAItem? RemoteItem => this.Item?.RemoteItem;
    public delegate void Update(int type, string? @params);
    public event Update OnUpdate;
    public bool IsPinned => this is StructureItemViewModel<StructureItem> item && (connection?.PinnedItems?.Contains(item) ?? false);
    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule && IsLocal;
    public bool CanOpenModule => HasModule && IsLocal;
    public bool CanDownloadModule => Item is Class && HasModule && !IsLocal;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item != null;
    public ItemType ItemType { get; private set; }

    [ObservableProperty] private string name;

    [ObservableProperty] private T? item;

    [ObservableProperty] private StructureItemViewModel<T>? parent;

    [ObservableProperty] private ImageSource? icon;

    [ObservableProperty] private IEnumerable<StructureItemViewModel<T>>? children;

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
                            Children = GetChildren(Item, this, logger, connection);

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
    public StructureItemViewModel() { }
    public StructureItemViewModel(T item, StructureItemViewModel<T>? parent = null, ILogger? logger = null, ConnectionModel? connection = null) : this()
    {
        this.logger = logger;
        this.connection = connection;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        updateCallback = Updated;
        GC.SuppressFinalize(updateCallback);
        NativeMethods.OdaServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), updateCallback);
        _ = SetExpanderAsync();
        _ = SetIconAsync();
    }
    ~StructureItemViewModel()
    {
        if (Item?.RemoteItem != null)
        {
            NativeMethods.OdaServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), null);
        }
        if (updateCallback != null)
        {
            GC.ReRegisterForFinalize(updateCallback);
        }
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
                .WithTimeout(TimeSpan.FromSeconds(10));
        }
        catch (TimeoutException)
        {
            logger?.Info($"Timeout when getting children for {this}");
            Children = null;
        }
        catch
        {
            logger?.Info($"Unhandled exception for {this}");
            Children = null;
            throw;
        }
    }

    private static IEnumerable<StructureItemViewModel<T>> GetChildren(T item, StructureItemViewModel<T>? parent = null, ILogger? logger = null, ConnectionModel? connection = null)
    {
        var items = item.FindConfigItems().OrderBy(x => x.Name).ToLookup(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution);

        IEnumerable<StructureItemViewModel<T>> children = items[true]
            .Select(child => new StructureItemViewModel<T>(child as T, parent, logger, connection))
            .OrderBy(x => x.Item?.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>();
        var workplaces = items[false].OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureItemViewModel<T>()
            {
                Name = "Workplaces",
                Icon = PredefinedImages.WorkplaceImage,
                Children = workplaces.Select(child => new StructureItemViewModel<T>(child as T, parent, logger, connection))
            };
            yield return workplace;
        }
        if (modules.Any())
        {
            var module = new StructureItemViewModel<T>()
            {
                Name = "Modules",
                Icon = PredefinedImages.ModuleImage,
                Children = modules.Select(child => new StructureItemViewModel<T>(child as T, parent, logger, connection))
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
            if (child.RemoteItem?.Id == item.RemoteItem.Id)
            {
                var rootItems = GetChildren(child.Item, parent, logger, connection);
                foreach (var rootItem in rootItems)
                {
                    yield return rootItem;
                }
                continue;
            }
            yield return child;
        }
    }

    private static IEnumerable<StructureItemViewModel<T>> ApplyCategories
        (IEnumerable<StructureItemViewModel<T>> structureItems, string subGroupParent = "")
    {
        var groups = structureItems
            .GroupBy(x => x.Item?.Category.SubstringAfter(subGroupParent).SubstringBefore("/"))
            .OrderBy(x => x.Key);
        foreach (var group in groups)
        {
            var rootItems = group.ToLookup(x => group.Key == x.Item?.Category.SubstringAfter(subGroupParent));
            yield return new StructureItemViewModel<T>()
            {
                Name = group.Key,
                Icon = PredefinedImages.FolderImage,
                Children = ApplyCategories(rootItems[false], $"{group.Key}/").Concat(rootItems[true])
            };
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
        await SetIconAsync();
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        OnPropertyChanged(nameof(Item));
        OnPropertyChanged(nameof(HasModule));
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
    public async Task OpenDirAsync()
    {
        try
        {
            if (Item?.Dir is null)
            {
                logger?.Info($"{Item} has no directory");
                return;
            }

            var dirPath = await Task.Run(Item.Dir.RemoteFolder.LoadFolder).ConfigureAwait(true);

            if (Directory.Exists(dirPath).Not())
            {
                logger?.Info($"Folder {dirPath} doesn't exist for {Item}");
                return;
            }

            Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath));
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }

    [RelayCommand]
    public void Pin()
    {
        try
        {
            if (Item is null)
            {
                logger?.Info("Can't pin this item");
                return;
            }
            if (!IsPinned)
            {
                var copy = new StructureItemViewModel<StructureItem>(Item, Parent as StructureItemViewModel<StructureItem>, logger, connection);
                if (copy.Item?.FullId is null || connection is null)
                    return;
                connection.PinnedItems.Add(copy);
                connection.AddinSettings.PinnedItems.Add(copy.Item.FullId);

            }
            else
            {
                if (connection is null)
                    return;
                connection.PinnedItems.Remove(x => x.Item?.FullId == Item.FullId);
                connection.AddinSettings.PinnedItems.Remove(Item.FullId);
            }
            _ = connection.AddinSettings.SaveAsync();
            OnPropertyChanged(nameof(IsPinned));
        }
        catch(Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }

    public override string ToString() => Name;
}