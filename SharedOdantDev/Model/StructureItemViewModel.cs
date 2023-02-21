using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using oda;
using odaCore;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OdantDev.Model;

[ObservableObject]
public partial class StructureItemViewModel<T> where T : StructureItem
{
    private static readonly object _lock = new object();
    private static List<StructureItemViewModel<T>> dummyList = new List<StructureItemViewModel<T>>() { new StructureItemViewModel<T>() };
    private ILogger logger;
    private IEnumerable<StructureItemViewModel<T>> children;
    private bool isLazyLoading;

    private delegate void Update(int Type, string Params);
    private event Update OnUpdate;
    private ServerApi.OnUpdate_CALLBACK Update_CALLBACK;
    private void Updated(int type, IntPtr Params)
    {
        if (type < 6)
        {
            RefreshCommand.Execute(this);
        }
        OnUpdate?.Invoke(type, Params == IntPtr.Zero ? String.Empty : Marshal.PtrToStringUni(Params));
    }

    [ObservableProperty]
    T item;

    [ObservableProperty]
    T parent;

    [ObservableProperty]
    string category;

    [ObservableProperty]
    int imageIndex;

    [ObservableProperty]
    bool isExpanded;
    partial void OnIsExpandedChanged(bool value)
    {
        if (value && isLazyLoading)
        {
            isLazyLoading = false;
            OnPropertyChanged("Children");
        }
    }
    public string Name => $"{Item?.Label ?? Item?.Name ?? Category}";
    public bool IsItemAvailable => Item != null;
    public bool HasChildren => Children.Any();
    public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(ImageIndex));
    public virtual IEnumerable<StructureItemViewModel<T>> Children
    {
        get => isLazyLoading ? dummyList : children;
        set { SetProperty(ref children, value); }
    }
    public ItemType ItemType { get; private set; }
    public StructureItemViewModel() { }
    public StructureItemViewModel(T item, bool lazyLoad, T parent = null, ILogger logger = null)
    {
        isLazyLoading = lazyLoad;
        this.logger = logger;
        Item = item;
        Parent = parent;
        ItemType = Item.ItemType;
        Category = ItemType.ToString();
        _ = InitChildrenAsync(item, lazyLoad, logger);
        Update_CALLBACK = Updated;
        GC.SuppressFinalize(Update_CALLBACK);
        ServerApi._SetOnUpdate(item.RemoteItem.GetIntPtr(), Update_CALLBACK);
        lock (_lock)
        {
            ImageIndex = Item.ImageIndex;
        }
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

    public async Task InitChildrenAsync(T item, bool lazyLoad, ILogger logger = null)
    {
        var task = Task.Run(() => GetChildren(item, lazyLoad, logger));
        if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))))
        {
            Children = await task;
        }
        else
        {
            logger?.Info($"Timeout when getting children for {item}");
            Children = null;
        }
    }

    public IEnumerable<StructureItemViewModel<T>> GetChildren(T item, bool lazyLoad, ILogger logger = null)
    {
        var items = item.getChilds(ItemType.All, Deep.Near).OfType<T>().AsParallel();
        var children = items
            .Where(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution)
            .Select(child => new StructureItemViewModel<T>(child, lazyLoad, item, logger));

        switch (item.ItemType)
        {
            case ItemType.Class:
            case ItemType.Module:
            case ItemType.Solution:
            case ItemType.Host:
                {
                    foreach (var viewItem in children)
                        yield return viewItem;
                    yield break;
                }
        };
        var modules = items.OfType<DomainModule>();
        var workplaces = items.OfType<DomainSolution>();
        if (workplaces.Any())
        {
            children = children.Prepend(
                new StructureItemViewModel<T>()
                {
                    Category = "Workplaces",
                    ImageIndex = Images.GetImageIndex(Icons.UserRole),
                    children = workplaces.Select(child => new StructureItemViewModel<T>(child as T, lazyLoad, item, logger))
                }).AsParallel();
        }
        if (modules.Any())
        {
            children = children.Prepend(
                new StructureItemViewModel<T>()
                {
                    Category = "Modules",
                    ImageIndex = Images.GetImageIndex(Icons.MagentaFolder),
                    children = modules.Select(child => new StructureItemViewModel<T>(child as T, lazyLoad, item, logger))
                }).AsParallel();
        }
        foreach (var viewItem in children)
        {
            yield return viewItem;
        }
    }

    [RelayCommand]
    public void Refresh()
    {
        if (Item == null) { return; }
        (Item as Class)?.ReloadClassFromServer();
        Item.Reset();
        this.Children = GetChildren(this.Item, isLazyLoading, logger);
        lock (_lock)
        {
            ImageIndex = Item.ImageIndex;
        }
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
    public override string ToString() => Name;
}