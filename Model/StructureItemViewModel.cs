using oda;
using OdantDev.Commands;
using odaServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
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
        private ServerApi.OnUpdate_CALLBACK Update_CALLBACK;
        private ILogger _logger;
        private IEnumerable<StructureItemViewModel<T>> children;
        private T item;
        private RelayCommand refreshCommand;
        private RelayCommand infoCommand;
        private int imageIndex;

        public string Name => $"{Item?.Label ?? Item?.Name ?? Category}";
        public RelayCommand RefreshCommand
        {
            get
            {
                return refreshCommand ??
                    (refreshCommand = new RelayCommand(obj =>
                    {
                        Refresh();
                        _logger?.Info($"{this} has been refreshed");
                    }));
            }
        }
        public RelayCommand InfoCommand => infoCommand ??
            (infoCommand = new RelayCommand(obj =>
            {
                if (Item == null) { return; }
                Clipboard.Clear();
                Clipboard.SetText(Item.FullId);
                _logger?.Info($"FullId copied to clipboard!");
            }));
        public bool IsItemAvailable => Item != null;
        public bool HasChildren => Children.Any();
        public virtual T Item { get => item; private set { item = value; NotifyPropertyChanged("Item"); } }
        public virtual string Category { get; set; }
        public virtual int ImageIndex { get => imageIndex; set { imageIndex = value; NotifyPropertyChanged("ImageIndex"); } }
        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(ImageIndex));
        public virtual IEnumerable<StructureItemViewModel<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public ItemType ItemType { get; private set; }
        public StructureItemViewModel() { }
        public StructureItemViewModel(T item, ILogger logger = null)
        {
            _logger = logger;
            Item = item;
            ItemType = Item.ItemType;
            Category = ItemType.ToString();
            Children = GetChildren(item, logger);
            Update_CALLBACK = Updated;
            ServerApi._SetOnUpdate(item.RemoteItem.GetIntPtr(), Update_CALLBACK);
            lock (_lock)
            {
                ImageIndex = Item.ImageIndex;
            }
        }
        public IEnumerable<StructureItemViewModel<T>> GetChildren(T item, ILogger logger = null)
        {
            var items = item.getChilds(ItemType.All, Deep.Near).OfType<T>().AsParallel().AsUnordered();
            var children = items
                .Where(x => x.ItemType != ItemType.Module && x.ItemType != ItemType.Solution)
                .Select(child => new StructureItemViewModel<T>(child, logger));

            if (item.ItemType == ItemType.Host || item.ItemType == ItemType.Class)
            {
                foreach (var viewItem in children)
                    yield return viewItem;
                yield break;
            };
            var modules = items.OfType<DomainModule>();
            var workplaces = items.OfType<DomainSolution>();
            if (modules.Any())
            {
                children = children.Prepend(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Modules",
                        ImageIndex = Images.GetImageIndex(Icons.MagentaFolder),
                        children = modules
                        .Select(child => new StructureItemViewModel<T>(child as T, logger))
                    }).AsParallel();
            }
            if (workplaces.Any())
            {
                children = children.Prepend(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Workplaces",
                        ImageIndex = Images.GetImageIndex(Icons.UserRole),
                        children = workplaces
                        .Select(child => new StructureItemViewModel<T>(child as T, logger))
                    }).AsParallel();
            }
            foreach(var viewItem in children)
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
            OnUpdate?.Invoke(type, Params == IntPtr.Zero? String.Empty : Marshal.PtrToStringUni(Params));
        }
        public void Refresh()
        {
            if (Item == null) { return; }
            Item.Reset();
            this.Children = GetChildren(this.Item, _logger);
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