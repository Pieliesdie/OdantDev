using oda;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        public virtual ImageSource Icon => Extension.ConvertToBitmapImage(Images.GetImage(Item.ImageIndex));
        public virtual IEnumerable<Node<T>> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
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
            this.Children = Item.getChilds(ItemType.All, Deep.Near).AsParallel().Cast<T>().Select(child => new Node<T>(child));
        }

        public override string ToString()
        {
            return $"{Item}";
        }
    }
}