using oda;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OdantDev
{
    public class Node<T> where T : StructureItem
    {
        public T Item { get; }

        public ImageSource Icon => Extension.ConvertToBitmapImage(Item?.Class?.Icon ?? Images.getImage(Item));


        public IEnumerable<Node<T>> Children { get; set; }

        public Node(T item)
        {
            Item = item;
        }

        public override string ToString()
        {
            return $"{Item}";
        }
    }
}