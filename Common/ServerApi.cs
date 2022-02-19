using oda;
using odaServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev
{
    internal static class ServerApi
    {
        public delegate void OnUpdate_CALLBACK(int Type, IntPtr Params);
        [DllImport("odaClient.dll", EntryPoint = "ODAItem_set_on_update", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void _SetOnUpdate(IntPtr item, OnUpdate_CALLBACK func);

        [DllImport("odaClient.dll", EntryPoint = "ODAItem_get_type", CallingConvention = CallingConvention.Cdecl)]
        public static extern int _GetType(IntPtr item);
        [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_item", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _GetItem(IntPtr list, int index);

        [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_release", CallingConvention = CallingConvention.Cdecl)]
        public static extern void _Release(IntPtr list);

        [DllImport("odaClient.dll", EntryPoint = "ODAItem_find_config_items", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr _FindConfigItems(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string xq);

        [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_length", CallingConvention = CallingConvention.Cdecl)]
        public static extern int _GetLength(IntPtr list);
        public static async Task<IEnumerable<T>> getChildsAsync<T>(this StructureItem item, ItemType itemType, Deep deep)
        {
            return await Task.Run(() => item.getChilds(itemType, deep).OfType<T>());
        }
        public static List<StructureItem> getChildren(this StructureItem structureItem, ItemType item_type, Deep deep)
        {
            string str1 = deep == Deep.Near ? "/" : "//";
            string str2 = item_type switch
            {
                ItemType.Module => "D[@t = 'MODULE']",
                ItemType.Base => "D[@t='ORGANIZATION' or @t='BASE']",
                _ => "*"
            };
            return FindConfigItems(structureItem, $".{str1}{str2}");
        }
        public static List<StructureItem> FindConfigItems(this StructureItem structureItem, string xq)
        {
            if (structureItem.RemoteItem == null) { return null; }
            IntPtr intPtr = structureItem.RemoteItem.GetIntPtr();
            IntPtr configItemsIntPtr = _FindConfigItems(intPtr, xq);
            int listLength = _GetLength(configItemsIntPtr);
            var ODAItems = Enumerable.Range(0, listLength)
                .AsParallel()
                .AsUnordered()
                .Select(x => CreateByType(_GetItem(configItemsIntPtr, x)))
                .ToList();
            try
            {
                _Release(configItemsIntPtr);
            }
            catch { }
            return ODAItems.AsParallel().AsUnordered().Select(x => ItemFactory.getStorageItem(x)).ToList();
        }
        public static ODAItem GetItem(IntPtr _ptr, int index) => CreateByType(ServerApi._GetItem(_ptr, index));
        public static ODAItem CreateByType(IntPtr item_ptr)
        {
            if (item_ptr == IntPtr.Zero)
                return null;
            ODAItem odaItem = null;
            switch (_GetType(item_ptr))
            {
                case 2:
                    odaItem = new ODAHost(item_ptr);
                    break;
                case 3:
                case 14:
                    odaItem = new ODADomain(item_ptr);
                    break;
                case 4:
                    odaItem = new ODAClass(item_ptr);
                    break;
                case 5:
                    odaItem = new ODAFolder(item_ptr);
                    break;
                case 6:
                    odaItem = new ODAIndex(item_ptr);
                    break;
                case 7:
                    odaItem = new ODAObject(item_ptr);
                    break;
                case 8:
                    odaItem = new ODAPack(item_ptr);
                    break;
                case 9:
                    odaItem = new ODAAsyncResult(item_ptr);
                    break;
                case 12:
                    odaItem = new ODARole(item_ptr);
                    break;
                case 13:
                    odaItem = new ODAUser(item_ptr);
                    break;
            }
            return odaItem;
        }
        public static IntPtr GetIntPtr(this ODAItem ODAItem)
        {
            return (IntPtr)typeof(ODAItem).GetField("_ptr", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ODAItem);
        }
    }
}
