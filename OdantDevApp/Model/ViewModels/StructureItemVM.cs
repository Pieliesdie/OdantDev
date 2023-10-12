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

using odaServer;

using SharedOdanDev.OdaOverride;

using SharedOdantDev.Common;

namespace OdantDev.Model;
public partial class StructureItemVM<T> : ObservableObject where T : StructureItem
{
    private static readonly IEnumerable<StructureItemVM<T>> dummyList = new List<StructureItemVM<T>>() { new() { Name = "Loading...", Icon = PredefinedImages.LoadImage } };
    private readonly ILogger logger;
    private readonly ConnectionModel connection;
    private bool isLoaded;
    private readonly NativeMethods.OdaServerApi.OnUpdate_CALLBACK Update_CALLBACK;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
    private async void Updated(int type, IntPtr Params)
    {
        try
        {
            switch (type)
            {
                case 3:
                {
                    if (Parent != null)
                    {
                        await this.Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                    }

                    break;
                }
                case 2:
                {
                    await this.RefreshAsync();
                    if (Parent != null)
                    {
                        await this.Parent.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                    }

                    break;
                }
                case < 6:
                    await this.RefreshAsync().WithTimeout(TimeSpan.FromSeconds(15));
                    break;
            }

            OnUpdate?.Invoke(type, Params == IntPtr.Zero ? String.Empty : Marshal.PtrToStringUni(Params));
        }
        catch (TimeoutException) { }
        catch (Exception ex)
        {
            logger?.Info(ex.Message);
        }
    }

    private ODAItem RemoteItem => this.Item?.RemoteItem;
    public delegate void Update(int Type, string Params);
    public event Update OnUpdate;
    public bool IsPinned => this is StructureItemVM<StructureItem> item && (connection?.PinnedItems?.Contains(item) ?? false);
    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule && IsLocal;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item != null;
    public ItemType ItemType { get; private set; }

    [ObservableProperty] private string name;

    [ObservableProperty] private T? item;

    [ObservableProperty] private StructureItemVM<T>? parent;

    [ObservableProperty] private ImageSource? icon;

    [ObservableProperty] private IEnumerable<StructureItemVM<T>>? children;

    [ObservableProperty] private bool isExpanded;

    async partial void OnIsExpandedChanged(bool value)
    {
        if (!value || isLoaded) return;
        isLoaded = true;
        if (Children is null || Children == dummyList)
        {
            await SetChildrenAsync(Item, logger, connection);
        }
    }
    public bool HasModule
    {
        get
        {
            switch (ItemType)
            {
                case ItemType.Class:
                    return (Item as Class)?.HasModule ?? false;
                case ItemType.Module:
                    {
                        if (Equals(Children, dummyList) || Children is null)
                            Children = GetChildren(Item, this, logger, connection);

                        return Children?.OfType<StructureItemVM<T>>().Any(x => x.HasModule) ?? false;
                    }
            }
            return false;
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
    public StructureItemVM() { }
    public StructureItemVM(T item, StructureItemVM<T> parent = null, ILogger logger = null, ConnectionModel connection = null) : this()
    {
        this.logger = logger;
        this.connection = connection;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Name = $"{RemoteItem?.Label ?? RemoteItem?.Name}";
        Update_CALLBACK = Updated;
        GC.SuppressFinalize(Update_CALLBACK);
        NativeMethods.OdaServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), Update_CALLBACK);
        _ = SetExpanderAsync();
        _ = SetIconAsync();
    }
    ~StructureItemVM()
    {
        if (Item?.RemoteItem != null)
        {
            NativeMethods.OdaServerApi._SetOnUpdate(Item.RemoteItem.GetIntPtr(), null);
        }
        if (Update_CALLBACK != null)
        {
            GC.ReRegisterForFinalize(Update_CALLBACK);
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

    private async Task SetChildrenAsync(T item, ILogger logger = null, ConnectionModel connection = null)
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
            logger?.Info($"Unhandled exepction for {this}");
            Children = null;
            throw;
        }
    }

    private IEnumerable<StructureItemVM<T>> GetChildren(T item, StructureItemVM<T> parent = null, ILogger logger = null, ConnectionModel connection = null)
    {
        var items = item.FindConfigItems().OrderBy(x => x.Name).ToLookup(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution);

        IEnumerable<StructureItemVM<T>> children = items[true]
            .Select(child => new StructureItemVM<T>(child as T, parent, logger, connection))
            .OrderBy(x => x.Item.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>();
        var workplaces = items[false].OfType<DomainSolution>();
        if (workplaces.Any())
        {
            var workplace = new StructureItemVM<T>()
            {
                Name = "Workplaces",
                Icon = PredefinedImages.WorkplaceImage,
                Children = workplaces.Select(child => new StructureItemVM<T>(child as T, parent, logger, connection))
            };
            yield return workplace;
        }
        if (modules.Any())
        {
            var module = new StructureItemVM<T>()
            {
                Name = "Modules",
                Icon = PredefinedImages.ModuleImage,
                Children = modules.Select(child => new StructureItemVM<T>(child as T, parent, logger, connection))
            };
            yield return module;
        }

        //create folders for categories
        if (item.ItemType == ItemType.Organization)
        {
            var emptyGroupedChildren = children.ToLookup(x => string.IsNullOrWhiteSpace(x.Item.Category));
            foreach (var groupedItem in StructureItemVM<T>.ApplyCategories(emptyGroupedChildren[false]))
                yield return groupedItem;
            children = emptyGroupedChildren[true];
        }
        //hide inner class in domain
        foreach (var child in children)
        {
            if (child.RemoteItem.Id == item.RemoteItem.Id)
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

    private static IEnumerable<StructureItemVM<T>> ApplyCategories
        (IEnumerable<StructureItemVM<T>> structureItems, string subGroupParent = "")
    {
        var groups = structureItems
            .GroupBy(x => x.Item?.Category.SubstringAfter(subGroupParent).SubstringBefore("/"))
            .OrderBy(x => x.Key);
        foreach (var group in groups)
        {
            var rootItems = group.ToLookup(x => group.Key == x.Item?.Category.SubstringAfter(subGroupParent));
            yield return new StructureItemVM<T>()
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
                logger.Info($"{Item} has no directory");
                return;
            }

            var dirPath = await Task.Run(Item.Dir.RemoteFolder.LoadFolder).ConfigureAwait(true);

            if (Directory.Exists(dirPath).Not())
            {
                logger.Info($"Folder {dirPath} doesn't exist for {Item}");
                return;
            }

            Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath));
        }
        catch (Exception ex)
        {
            logger.Info(ex.ToString());
        }
    }

    [RelayCommand]
    public void Pin()
    {
        if (!IsPinned)
        {
            var copy = new StructureItemVM<StructureItem>(Item, Parent as StructureItemVM<StructureItem>, logger, connection);
            connection.PinnedItems.Add(copy);
            connection.AddinSettings.PinnedItems.Add(copy.Item.FullId);
        }
        else
        {
            connection.PinnedItems.Remove(x => x.Item.FullId == this.Item.FullId);
            connection.AddinSettings.PinnedItems.Remove(Item.FullId);
        }
        _ = connection.AddinSettings.SaveAsync();
        OnPropertyChanged(nameof(IsPinned));
    }

    public override string ToString() => Name;
}