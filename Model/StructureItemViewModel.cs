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
        static readonly object _lock = new object();
        private ILogger _logger;
        private IEnumerable<StructureItemViewModel<T>> children;
        private T item;
        private RelayCommand refreshCommand;
        public string Name => $"{Item?.ToString() ?? Category}";
        public RelayCommand RefreshCommand
        {
            get
            {
                return refreshCommand ??
                    (refreshCommand = new RelayCommand(obj =>
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
                        _logger?.Info($"{this} has been refreshed");
                    }));
            }
        }
        private RelayCommand infoCommand;
        private int imageIndex;

        public bool IsItemAvailable => Item != null;
        public bool HasChildren => Children.Any();
        public RelayCommand InfoCommand
        {
            get
            {
                return infoCommand ??
                    (infoCommand = new RelayCommand(obj =>
                    {
                        if (Item == null) { return; }
                        Clipboard.Clear();
                        Clipboard.SetText(Item.FullId);
                        _logger?.Info($"FullId copied to clipboard!");
                    }));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public virtual T Item { get => item; private set { item = value; NotifyPropertyChanged("Item"); } }
        public virtual string Category { get; set; }
        public virtual int ImageIndex { get => imageIndex; set { imageIndex = value; NotifyPropertyChanged("ImageIndex"); } }
        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(ImageIndex));
        public virtual IEnumerable<StructureItemViewModel<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public ItemType ItemType { get; private set; }
        public StructureItemViewModel() { }
        public StructureItemViewModel(T item, ILogger logger = null)
        {
            this.item = item;
            _logger = logger;

            ItemType = Item.ItemType;
            Category = ItemType.ToString();
            children = GetChildren(item, logger);
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

            if (item.ItemType == ItemType.Host || item.ItemType == ItemType.Class) { return children; };
            var modules = items.Where(x => x.ItemType == ItemType.Module);
            var workplaces = items.Where(x => x.ItemType == ItemType.Solution);
            if (modules.Any())
            {
                children = children.Append(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Modules",
                        ImageIndex = Images.GetImageIndex(Icons.MagentaFolder),
                        children = items.Where(x => x.ItemType == ItemType.Module)
                        .Select(child => new StructureItemViewModel<T>(child, logger))
                    }).AsParallel();
            }
            if (workplaces.Any())
            {
                children = children.Append(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Workplaces",
                        ImageIndex = Images.GetImageIndex(Icons.UserRole),
                        children = items.Where(x => x.ItemType == ItemType.Solution)
                        .Select(child => new StructureItemViewModel<T>(child, logger))
                    }).AsParallel();
            }
            return children;
        }
        public override string ToString()
        {
            return $"{Item?.ToString() ?? Category}";
        }
    }
}