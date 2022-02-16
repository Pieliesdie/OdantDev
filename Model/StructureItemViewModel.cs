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
                        this.Children = GetChildren(this.Item, _logger);
                        NotifyPropertyChanged("Name");
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

        public IEnumerable<StructureItemViewModel<T>> GetChildren(T item, ILogger logger = null)
        {
            var items = getChildren(item, ItemType.All, Deep.Near).OfType<T>();
            var children = items
                .Where(x => x.ItemType != ItemType.Module)
                .Select(child => new StructureItemViewModel<T>(child, logger));
            var modules = items
                .Where(x => x.ItemType == ItemType.Module);
            if (modules.Any())
            {
                children = children.Append(
                    new StructureItemViewModel<T>()
                    {
                        Category = "Модули",
                        Children = modules.Select(child => new StructureItemViewModel<T>(child, logger))
                    }).AsParallel();
            }
            return children;
        }
        public IEnumerable<StructureItem> getChildren(T structureItem, ItemType item_type, Deep deep)
        {
            string str1 = item_type == ItemType.Base? "D[@t='ORGANIZATION' or @t='BASE']" : "*";
            string str2 = deep == Deep.Near ? "/" : "//";
            return this.FindConfigItems(structureItem, $".{str2}{str1}");
        }
        /*private void OnUpdate(int t, IntPtr intPtr)
        {
            var message = Marshal.PtrToStringUni(intPtr);
        }*/
        public IEnumerable<StructureItem> FindConfigItems(T structureItem, string xq)
        {
            if (structureItem.RemoteItem == null) { yield break; }
            IntPtr intPtr = (IntPtr)typeof(ODAItem).GetField("_ptr", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(structureItem.RemoteItem);
            //ServerApi._SetOnUpdate(intPtr, OnUpdate);
            IntPtr configItemsIntPtr = ServerApi._FindConfigItems(intPtr, xq);
            int listLength = ServerApi._GetLength(configItemsIntPtr);
            var ODAItems = Enumerable.Range(0, listLength).AsParallel().AsUnordered().Select(x => ServerApi.CreateByType(ServerApi._GetItem(configItemsIntPtr, x)));
            foreach(var item in ODAItems)
            {
                yield return ItemFactory.getStorageItem(item);
            }
            try
            {
                ServerApi._Release(configItemsIntPtr);
            }
            catch { }
        }
        public override string ToString()
        {
            return $"{Item?.ToString() ?? Category}";
        }
    }
}