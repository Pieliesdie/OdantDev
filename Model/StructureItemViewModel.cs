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
                        if(Item == null) { return; }
                        Item.Reset();
                        this.Children = GetChildren(this.Item);
                        NotifyPropertyChanged("Item");
                        NotifyPropertyChanged("Icon");
                    }));
            }
        }

        private RelayCommand infoCommand;

        public bool IsItemAvailable => Item != null;
        public RelayCommand InfoCommand
        {
            get
            {
                return infoCommand ??
                    (infoCommand = new RelayCommand(obj =>
                    {
                        if(Item == null) { return; }
                        Clipboard.Clear();
                        Clipboard.SetText(Item.FullId);
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
        public StructureItemViewModel() { }

        public StructureItemViewModel(T item)
        {
            Item = item;
            Category = item.ItemType.ToString();
            Children = GetChildren(item);
        }
        public static IEnumerable<StructureItemViewModel<T>> GetChildren(T item)
        {
            var items = item.getChilds(ItemType.All, Deep.Near).AsParallel().OfType<T>();
            IEnumerable<StructureItemViewModel<T>> children = items
                .Where(x => x.ItemType != ItemType.Module)
                .Select(child => new StructureItemViewModel<T>(child));
            var modules = items
                .Where(x => x.ItemType == ItemType.Module);
            if (modules.Any())
            {
                children = children.Append(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Модули",
                        Children = modules.Select(child => new StructureItemViewModel<T>(child))
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