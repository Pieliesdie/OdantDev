using oda;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OdantDev
{
    public class Node<T> : INotifyPropertyChanged where T : StructureItem
    {
        private IEnumerable<Node<T>> children;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public T Item { get; private set; }

        public string Category { get; private set; }

        public ImageSource Icon => Extension.ConvertToBitmapImage(Item?.Class?.Icon ?? Images.getImage(Item));

        public IEnumerable<Node<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public Node() { }

        public Node(T item)
        {
            Item = item;
            //Tree with groups
            /*this.Children = item.getChilds(ItemType.All, Deep.Near)
                .Cast<T>()
                .AsParallel()
                .GroupBy(x => x.TypeLabel)
                .Select(x => new Node<T>() { Category = x.Key.ToString(), Children = x.Select(y => new Node<T>(y)) } );*/
            this.Children = item.getChilds(ItemType.All, Deep.Near).AsParallel().Cast<T>().Select(child => new Node<T>(child));
        }

        public override string ToString()
        {
            return Category == null ? $"{Item}" : $"{Category}/{Item}";
        }
    }
}