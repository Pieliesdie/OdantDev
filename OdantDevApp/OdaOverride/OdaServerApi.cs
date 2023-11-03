using System;
using System.Runtime.InteropServices;

namespace NativeMethods;

internal static class OdaServerApi
{
    public delegate void OnUpdate_CALLBACK(int Type, IntPtr Params);
    [DllImport("odaClient.dll", EntryPoint = "ODAItem_set_on_update", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetOnUpdate(IntPtr item, OnUpdate_CALLBACK func);

    [DllImport("odaClient.dll", EntryPoint = "ODAItem_get_type", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetType(IntPtr item);
    [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_item", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr GetItem(IntPtr list, int index);

    [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_release", CallingConvention = CallingConvention.Cdecl)]
    public static extern void Release(IntPtr list);

    [DllImport("odaClient.dll", EntryPoint = "ODAItem_find_config_items", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FindConfigItems(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string xq);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODAServer_hosts", ExactSpelling = true)]
    public static extern IntPtr Hosts(IntPtr server);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "ODADomain_domains", ExactSpelling = true)]
    public static extern IntPtr Domains(IntPtr domain);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "ODAItem_find_item", ExactSpelling = true)]
    public static extern IntPtr FindItem(IntPtr item, [MarshalAs(UnmanagedType.LPWStr)] string search_path);

    [DllImport("odaClient.dll", EntryPoint = "ODAVariantsList_get_length", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetLength(IntPtr list);

    [DllImport("odaClient.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode, EntryPoint = "ODADomain_create_domain2", ExactSpelling = true)]
    public static extern IntPtr Create_Domain(IntPtr domain, [MarshalAs(UnmanagedType.LPWStr)] string name, [MarshalAs(UnmanagedType.LPWStr)] string type);
}
