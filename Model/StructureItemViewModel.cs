using oda;
using OdantDev.Commands;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OdantDev.Model
{
    public class StructureItemViewModel<T> : INotifyPropertyChanged where T : StructureItem
    {
        private ILogger _logger;
        private IEnumerable<StructureItemViewModel<T>> children;
        private T item;
        private RelayCommand refreshCommand;
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
                        NotifyPropertyChanged("Item");
                        NotifyPropertyChanged("Icon");
                        _logger?.Info($"{this} has been refreshed");
                    }));
            }
        }
        private RelayCommand infoCommand;
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
        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(Item?.ImageIndex ?? Images.GetImageIndex(Icons.Module)));
        public virtual IEnumerable<StructureItemViewModel<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public ItemType ItemType { get; private set; }
        public StructureItemViewModel() { }
        public StructureItemViewModel(T item, ILogger logger = null)
        {
            Item = item;
            _logger = logger;
            ItemType = item.ItemType;
            Category = item.ItemType.ToString();
            Children = GetChildren(item, logger);
        }
        public static IEnumerable<StructureItemViewModel<T>> GetChildren(T item, ILogger logger = null)
        {
            var items = item.getChilds(ItemType.All, Deep.Near).AsParallel().OfType<T>();
            IEnumerable<StructureItemViewModel<T>> children = items
                .Where(x => x.ItemType != ItemType.Module)
                .Select(child => new StructureItemViewModel<T>(child,logger));
            var modules = items
                .Where(x => x.ItemType == ItemType.Module);
            if (modules.Any())
            {
                children = children.Append(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Модули",
                        Children = modules.Select(child => new StructureItemViewModel<T>(child,logger))
                    });
            }
            return children;
        }
        public override string ToString()
        {
            return $"{Item?.ToString() ?? Category}";
        }
    }
}