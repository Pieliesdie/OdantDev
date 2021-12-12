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
    public class Node<T> where T : StructureItem
    {

        public event PropertyChangedEventHandler PropertyChanged;
        public T Item { get; }

        public ImageSource Icon => Extension.ConvertToBitmapImage(Item?.Class?.Icon ?? Images.getImage(Item));


        public IEnumerable<Node<T>> Children { get; set; }

        public Node(T item)
        {
            Item = item;
            this.Children = item.getChilds(ItemType.All, Deep.Near).Cast<T>().AsParallel().Select(child => new Node<T>(child));
        }

        public override string ToString()
        {
            return $"{Item}";
        }
        private void NotifyPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}