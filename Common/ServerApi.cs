using odaServer;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static ODAItem GetItem(IntPtr _ptr, int index) => CreateByType(ServerApi._GetItem(_ptr, index));

        public static ODAItem CreateByType(IntPtr item_ptr)
        {
            if (item_ptr == IntPtr.Zero)
                return (ODAItem)null;
            ODAItem odaItem = (ODAItem)null;
            switch (ServerApi._GetType(item_ptr))
            {
                case 2:
                    odaItem = (ODAItem)new ODAHost(item_ptr);
                    break;
                case 3:
                case 14:
                    odaItem = (ODAItem)new ODADomain(item_ptr);
                    break;
                case 4:
                    odaItem = (ODAItem)new ODAClass(item_ptr);
                    break;
                case 5:
                    odaItem = (ODAItem)new ODAFolder(item_ptr);
                    break;
                case 6:
                    odaItem = (ODAItem)new ODAIndex(item_ptr);
                    break;
                case 7:
                    odaItem = (ODAItem)new ODAObject(item_ptr);
                    break;
                case 8:
                    odaItem = (ODAItem)new ODAPack(item_ptr);
                    break;
                case 9:
                    odaItem = (ODAItem)new ODAAsyncResult(item_ptr);
                    break;
                case 12:
                    odaItem = (ODAItem)new ODARole(item_ptr);
                    break;
                case 13:
                    odaItem = (ODAItem)new ODAUser(item_ptr);
                    break;
            }
            return odaItem;
        }
    }
}
