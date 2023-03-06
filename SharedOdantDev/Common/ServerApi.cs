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
    public static class ServerApi
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

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

        public static IEnumerable<StructureItem> FindConfigItems(this StructureItem structureItem)
        {
            if (structureItem.RemoteItem == null) { yield break; }
            var xq = GetConfigFilter(structureItem);

            IntPtr intPtr = structureItem.RemoteItem.GetIntPtr();
            IntPtr configItemsIntPtr = _FindConfigItems(intPtr, xq);
            int listLength = _GetLength(configItemsIntPtr);
            var ODAItems = Enumerable.Range(0, listLength)
                .Select(x => oda.OdaOverride.ItemFactory.GetStorageItem(CreateByType(_GetItem(configItemsIntPtr, x))));
            foreach (var item in ODAItems)
            {
                yield return item;
            }
            _Release(configItemsIntPtr);
        }
        private static string GetConfigFilter(StructureItem item)
        {
            if (item.ItemType == ItemType.Host)
            {
                return "./(D[@i = 'ROOT'], D[@i = 'ROOT']/D[@i = ('DEVELOPE', 'WORK')])";
            }

            if (item.Id == "ROOT")
            {
                return "./(C[@i='ROOT']/*[not(@t=('SOLUTION', 'WORKPLACE'))], *[not(@i = (../@i, '000000000000000', 'SYSTEM', 'DEVELOPE', 'WORK')) and not(@t=('SOLUTION', 'WORKPLACE'))])";
            }

            return "./(*[not(@i = (../@i, '000000000000000', 'SYSTEM')) and not(@t=('SOLUTION', 'WORKPLACE'))], *[@i = ../@i]/*)";
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
