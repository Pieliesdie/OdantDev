using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NativeMethods;

internal static class OdaServerApi
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

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODAServer_hosts", ExactSpelling = true)]
    public static extern IntPtr _Hosts(IntPtr server);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODADomain_domains", ExactSpelling = true)]
    public static extern IntPtr _Domains(IntPtr domain);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "ODAItem_find_item", ExactSpelling = true)]
    public static extern IntPtr _FindItem(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string search_path);

    [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_length", CallingConvention = CallingConvention.Cdecl)]
    public static extern int _GetLength(IntPtr list);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "ODADomain_create_domain2", ExactSpelling = true)]
    public static extern IntPtr _Create_Domain(IntPtr domain, [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string type);
}
