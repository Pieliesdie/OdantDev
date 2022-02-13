using oda;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OdantDev.Model
{
    public class Node<T> : INotifyPropertyChanged where T : StructureItem
    {
        private IEnumerable<Node<T>> children;
        private T item;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public virtual T Item { get => item; private set { item = value; NotifyPropertyChanged("Item"); } }

        public virtual string Category { get; set; }

        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(Item?.ImageIndex ?? Images.GetImageIndex(Icons.Module)));
        public virtual IEnumerable<Node<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public Node() { }

        public Node(T item)
        {
            Item = item;
            Category = item.ItemType.ToString();

            var children = Item.getChilds(ItemType.All, Deep.Near).AsParallel().OfType<T>();
            this.Children = children
                .Where(x => x.ItemType != ItemType.Module)
                .Select(child => new Node<T>(child));
            var modules = children
                .Where(x => x.ItemType == ItemType.Module);
            if (modules.Any())
            {
                this.Children = this.Children.Append(
                    new Node<T>() 
                    { 
                        Category = "Модули",
                        Children = modules.Select(child => new Node<T>(child)) 
                    });
            }
        }
        public override string ToString()
        {
            return $"{Item?.ToString() ?? Category}";
        }
    }
}