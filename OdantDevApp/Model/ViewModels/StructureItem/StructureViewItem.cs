using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OdantDevApp.Model.Git;
using OdantDevApp.Model.ViewModels.Settings;
using odaServer;
using SharedOdanDev.OdaOverride;
using SharedOdantDev.Common;
#pragma warning disable CA1860

namespace OdantDevApp.Model.ViewModels;

public partial class StructureViewItem<T> : ObservableObject where T : StructureItem
{
    private static readonly List<StructureViewItem<T>> dummyList = [new() { Name = "Loading...", Icon = PredefinedImages.LoadImage }];

    private readonly ILogger? logger;
    private readonly ConnectionModel? connection;
    private bool isLoaded;
    private readonly NativeMethods.OdaServerApi.OnUpdate_CALLBACK? updateCallback;

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
            //ignore
        }
        catch (Exception err)
        {
            logger?.LogInformation(err, "Error in 'Updated': {Error}", err.Message);
        }
    }

    private ODAItem? RemoteItem => Item?.RemoteItem;

    public bool IsPinned => this is StructureViewItem<StructureItem> item &&
                            (connection?.PinnedItems?.Contains(item) ?? false);

    public bool HasRepository => !string.IsNullOrWhiteSpace(Item?.Root?.GetAttribute("GitLabRepository"));
    public bool CanCreateModule => Item is Class && !HasModule && IsLocal;
    public bool CanCreateDeveloper => ItemType == oda.ItemType.DevPart && IsLocal;
    public bool CanOpenModule => HasModule && IsLocal;
    public bool CanDownloadModule => Item is Class && HasModule && !IsLocal;
    public bool CanCreateRepo => GitClient.Client?.HostUrl != null;
    public bool IsLocal => Item?.Host?.IsLocal ?? false;
    public bool IsItemAvailable => Item is { IsDisposed: false };
    public ItemType? ItemType => Item?.ItemType;

    [ObservableProperty] public partial string Name { get; set; }

    [ObservableProperty] public partial T? Item { get; set; }

    [ObservableProperty] public partial StructureViewItem<T>? Parent { get; set; }

    [ObservableProperty] public partial ImageSource? Icon { get; set; }

    [ObservableProperty] public partial IEnumerable<StructureViewItem<T>>? Children { get; set; }

    [ObservableProperty] public partial bool IsExpanded { get; set; }

    async partial void OnIsExpandedChanged(bool value)
    {
        try
        {
            if (!value || isLoaded)
            {
                return;
            }

            isLoaded = true;
            if (Children is not null && !Children.Equals(dummyList))
            {
                return;
            }

            if (Item != null)
            {
                await SetChildrenAsync();
            }
        }
        catch (Exception err)
        {
            logger?.LogError(err, "Error 'OnIsExpandedChanged': {Error}", err.Message);
        }
    }

    public bool HasModule
    {
        get
        {
            if (Item is null)
                return false;
            switch (ItemType)
            {
                case oda.ItemType.Class:
                    return (Item as Class)?.HasModule ?? false;
                case oda.ItemType.Module:
                {
                    if (Children is null || Children.Equals(dummyList))
                    {
                        Children = GetChildren().ToArray();
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
            if (!hasChildren && ItemType != oda.ItemType.Class)
            {
                hasChildren = Item.Class?.RemoteItem?.HasChilds ?? false;
            }

            return hasChildren;
        }
    }

    public StructureViewItem(
        StructureViewItem<T>? parent = null,
        ILogger? logger = null,
        ConnectionModel? connection = null
    )
    {
        this.logger = logger;
        this.connection = connection;
        Parent = parent;
    }

    public StructureViewItem(
        T item,
        StructureViewItem<T>? parent = null,
        ILogger? logger = null,
        ConnectionModel? connection = null
    ) : this(parent, logger, connection)
    {
        Item = item;
        Name = $"{item.RemoteItem?.Label ?? item.RemoteItem?.Name}";

        if (Item.RemoteItem is not null)
        {
            updateCallback = Updated;
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

    private async Task SetChildrenAsync()
    {
        try
        {
            Children = await Task
                .Run(() => GetChildren().ToArray())
                .WithTimeout(TimeSpan.FromSeconds(AddinSettings.Instance.StructureItemTimeout));
        }
        catch (TimeoutException)
        {
            logger?.LogInformation("Timeout when getting children for {StructureViewItem}", this);
            Children = null;
            await SetExpanderAsync();
        }
        catch
        {
            logger?.LogInformation("Unhandled exception for {StructureViewItem}", this);
            Children = null;
            throw;
        }
    }

    public IEnumerable<StructureViewItem<T>> GetChildren()
    {
        return GetChildrenCore()
            .ToArray()
            .GroupBy(x => x.Item?.FullId ?? x.Name)
            .Select(x => x.Last());
    }

    private IEnumerable<StructureViewItem<T>> GetChildrenCore()
    {
        if (Item == null)
        {
            yield break;
        }

        var items = Item
            .FindConfigItems()
            .OrderBy(x => x.Name)
            .ToLookup(x => x.ItemType != oda.ItemType.Module && x.ItemType != oda.ItemType.Solution);

        var children = items[true]
            .OfType<T>()
            .Select(child => new StructureViewItem<T>(child, this, logger, connection))
            .OrderBy(x => x.Item?.SortIndex)
            .ThenBy(x => x.Name);

        //create folders for modules and workplaces
        var modules = items[false].OfType<DomainModule>().ToArray();
        var workplaces = items[false].OfType<DomainSolution>().ToArray();
        if (workplaces.Any())
        {
            var workplace = new StructureViewItem<T>(this, logger, connection)
            {
                Name = "Workplaces",
                Icon = PredefinedImages.WorkplaceImage,
                Children = workplaces
                    .OfType<T>()
                    .Select(child => new StructureViewItem<T>(child, this, logger, connection))
            };
            yield return workplace;
        }

        if (modules.Any())
        {
            var module = new StructureViewItem<T>(this, logger, connection)
            {
                Name = "Modules",
                Icon = PredefinedImages.ModuleImage,
                Children = modules
                    .OfType<T>()
                    .Select(child => new StructureViewItem<T>(child, this, logger, connection))
            };
            yield return module;
        }

        //hide inner class in domain
        foreach (var child in children)
        {
            if (child.RemoteItem?.Id == Item.RemoteItem.Id)
            {
                var childPtr = child.RemoteItem?.GetIntPtr();

                if (childPtr is null || child.Item is null)
                {
                    continue;
                }

                if (updateCallback != null)
                {
                    NativeMethods.OdaServerApi.SetOnUpdate(childPtr.Value, updateCallback);
                }

                var rootItems = child
                    .GetChildren()
                    .Select(x =>
                    {
                        x.Parent = this;
                        return x;
                    });

                //create folders for categories
                if (Item.ItemType == oda.ItemType.Organization)
                {
                    rootItems = ApplyCategories(rootItems);
                }

                foreach (var rootItem in rootItems)
                {
                    rootItem.Parent = this;
                    yield return rootItem;
                }


                continue;
            }

            yield return child;
        }
    }

    private IEnumerable<StructureViewItem<T>> ApplyCategories(
        IEnumerable<StructureViewItem<T>> items,
        string subGroupParent = ""
    )
    {
        var emptyGroupedChildren = items.ToLookup(x => string.IsNullOrWhiteSpace(x.Item?.Category));

        var groups = emptyGroupedChildren[false]
            .GroupBy(x => x.Item?.Category.SubstringAfter(subGroupParent).SubstringBefore("/"))
            .OrderBy(x => x.Key);
        foreach (var group in groups)
        {
            var rootItems = group.ToLookup(x => group.Key == x.Item?.Category.SubstringAfter(subGroupParent));
            yield return new StructureViewItem<T>(this, logger, connection)
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
            logger?.LogInformation(e, "Error in 'Item.Reset()': {Error}", e.Message);
        }

        try
        {
            (Item as Class)?.ReloadClassFromServer();
        }
        catch (Exception e)
        {
            logger?.LogInformation(e, "Error in 'Item.ReloadClassFromServer()': {Error}", e.Message);
        }

        try
        {
            (Item as Domain)?.Class.ReloadClassFromServer();
        }
        catch (Exception e)
        {
            logger?.LogInformation(e, "Error in 'Item.ReloadClassFromServer()': {Error}", e.Message);
        }

        if (isLoaded || force)
        {
            await SetChildrenAsync();
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

    [RelayCommand]
    public void Info()
    {
        if (Item == null)
        {
            return;
        }

        Clipboard.Clear();
        Clipboard.SetText(Item.FullId);
        logger?.LogInformation("FullId copied to clipboard!");
    }

    [RelayCommand]
    public async Task OpenDirAsync()
    {
        try
        {
            if (Item?.Dir is null)
            {
                logger?.LogInformation("{StructureItem} has no directory", Item);
                return;
            }

            var dirPath = await Task.Run(Item.Dir.RemoteFolder.LoadFolder).ConfigureAwait(true);

            if (Directory.Exists(dirPath).Not())
            {
                logger?.LogInformation("Folder {DirPath} doesn't exist for {StructureItem}", dirPath, Item);
                return;
            }

            Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath));
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex, "Error in 'OpenDirAsync()': {Error}", ex.Message);
        }
    }

    [RelayCommand]
    public void Pin()
    {
        try
        {
            if (Item is null)
            {
                logger?.LogInformation("Can't pin this item");
                return;
            }

            if (!IsPinned)
            {
                var copy = new StructureViewItem<StructureItem>(
                    Item,
                    Parent as StructureViewItem<StructureItem>,
                    logger, connection
                );
                if (copy.Item?.FullId is null || connection is null)
                    return;
                connection.PinnedItems.Add(copy);
                connection.AddinSettings.PinnedItems?.Add(copy.Item.FullId);
            }
            else
            {
                if (connection is null)
                    return;
                connection.PinnedItems.Remove(x => x.Item?.FullId == Item.FullId);
                connection.AddinSettings.PinnedItems?.Remove(Item.FullId);
            }

            _ = connection.AddinSettings.SaveAsync();
            OnPropertyChanged(nameof(IsPinned));
        }
        catch (Exception ex)
        {
            logger?.LogInformation(ex, "Error in 'Pin()': {Error}", ex.Message);
        }
    }

    public override string ToString() => Name;
}