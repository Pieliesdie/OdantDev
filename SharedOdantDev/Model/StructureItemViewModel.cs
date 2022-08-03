using oda;
using OdantDev.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OdantDev.Model
{
    public class StructureItemViewModel<T> : INotifyPropertyChanged where T : StructureItem
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public delegate void Update(int Type, string Params);
        public event Update OnUpdate;

        private static readonly object _lock = new object();
        public ServerApi.OnUpdate_CALLBACK Update_CALLBACK;
        private ILogger logger;
        private IEnumerable<StructureItemViewModel<T>> children;
        private T item;
        private T parent;
        private RelayCommand refreshCommand;
        private RelayCommand infoCommand;
        private int imageIndex;
        private bool isExpanded;
        public string Name => $"{Item?.Label ?? Item?.Name ?? Category}";
        public RelayCommand RefreshCommand
        {
            get
            {
                return refreshCommand ??
                    (refreshCommand = new RelayCommand(obj =>
                    {
                        Refresh();
                        logger?.Info($"{this} has been refreshed");
                    }));
            }
        }
        public RelayCommand InfoCommand => infoCommand ??
            (infoCommand = new RelayCommand(obj =>
            {
                if (Item == null) { return; }
                Clipboard.Clear();
                Clipboard.SetText(Item.FullId);
                logger?.Info($"FullId copied to clipboard!");
            }));
        public bool IsItemAvailable => Item != null;
        public bool HasChildren => Children.Any();
        public virtual T Item { get => item; private set { item = value; NotifyPropertyChanged("Item"); } }
        public virtual T Parent { get => parent; private set { parent = value; NotifyPropertyChanged("Parent"); } }
        public virtual string Category { get; set; }

        public virtual int ImageIndex
        {
            get => imageIndex;
            set
            {
                imageIndex = value;
                NotifyPropertyChanged("ImageIndex");
            }
        }

        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(ImageIndex));
        private bool isLazyLoading;
        private List<StructureItemViewModel<T>> dummyList => new List<StructureItemViewModel<T>>() { new StructureItemViewModel<T>() };
        public virtual IEnumerable<StructureItemViewModel<T>> Children
        {
            get => isLazyLoading ? dummyList : children;
            set { children = value; NotifyPropertyChanged("Children"); }
        }
        public ItemType ItemType { get; private set; }
        public bool IsExpanded
        {
            get { return isExpanded; }
            set
            {
                if (value != isExpanded)
                {
                    isExpanded = value;
                    if (value && isLazyLoading)
                    {
                        isLazyLoading = false;
                        NotifyPropertyChanged("Children");
                    }
                    this.NotifyPropertyChanged("IsExpanded");
                }
            }
        }
        public StructureItemViewModel() { }
        public StructureItemViewModel(T item, bool lazyLoad, T parent = null, ILogger logger = null)
        {
            isLazyLoading = lazyLoad;
            this.logger = logger;
            Item = item;
            Parent = parent;
            ItemType = Item.ItemType;
            Category = ItemType.ToString();
            InitChildrenAsync(item, lazyLoad, logger);
            Update_CALLBACK = Updated;
            GC.SuppressFinalize(Update_CALLBACK);
            ServerApi._SetOnUpdate(item.RemoteItem.GetIntPtr(), Update_CALLBACK);
            lock (_lock)
            {
                ImageIndex = Item.ImageIndex;
            }
        }

        public async Task InitChildrenAsync(T item, bool lazyLoad, ILogger logger = null)
        {
            var task = Task.Run(() => GetChildren(item, lazyLoad, logger));
            if(task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))))
            {
                Children = await task;
            }
            else
            {
                logger?.Info($"Timeout when getting children for {item}");
                Children = null;
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
        public IEnumerable<StructureItemViewModel<T>> GetChildren(T item, bool lazyLoad, ILogger logger = null)
        {
            var items = item.getChilds(ItemType.All, Deep.Near).OfType<T>().AsParallel().AsUnordered();
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
        private void Updated(int type, IntPtr Params)
        {
            if (type < 6)
            {
                RefreshCommand.Execute(this);
            }
            OnUpdate?.Invoke(type, Params == IntPtr.Zero ? String.Empty : Marshal.PtrToStringUni(Params));
        }
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
            NotifyPropertyChanged("Name");
            NotifyPropertyChanged("Item");
            NotifyPropertyChanged("Icon");
        }
        public override string ToString()
        {
            return Name;
        }
    }
}