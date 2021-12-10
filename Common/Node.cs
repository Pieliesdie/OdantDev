using oda;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
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

        public static Node<T> GetChildren(T root)
        {
            var children = root.getChilds(ItemType.All, Deep.Near);
            var rootNode = new Node<T>(root);
            rootNode.Children = children?.Cast<T>().AsParallel().Select(child => GetChildren(child));
            return rootNode;
        }
        public static async Task<Node<T>> GetChildrenAsync(T root)
        {
            return await Task.Run(() => { return GetChildren(root); });
        }
    }
}