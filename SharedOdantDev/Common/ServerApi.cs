using oda;

using odaServer;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev;

public static class ServerApi
{
    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

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

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODAServer_hosts", ExactSpelling = true)]
    public static extern IntPtr _Hosts(IntPtr server);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODADomain_domains", ExactSpelling = true)]
    public static extern IntPtr _Domains(IntPtr domain);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "ODAItem_find_item", ExactSpelling = true)]
    public static extern IntPtr _FindItem(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string search_path);

    [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_length", CallingConvention = CallingConvention.Cdecl)]
    public static extern int _GetLength(IntPtr list);
}
