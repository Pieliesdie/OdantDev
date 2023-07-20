using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using oda;
using odaServer;

namespace OdantDev;
public static class StructureItemEx
{
    private static IEnumerable<T> ReadList<T>(IntPtr intPtr)
    {
        int listLength = NativeMethods.OdaServerApi._GetLength(intPtr);
        var ODAItems = Enumerable.Range(0, listLength)
            .Select(x => oda.OdaOverride.ItemFactory.GetStorageItem(CreateByType(NativeMethods.OdaServerApi._GetItem(intPtr, x))))
            .OfType<T>()
            .ToHashSet();
        NativeMethods.OdaServerApi._Release(intPtr);
        return ODAItems;
    }
    public static IEnumerable<StructureItem> FindConfigItems(this StructureItem structureItem)
    {
        if (structureItem.RemoteItem == null) { return Enumerable.Empty<StructureItem>(); }

        var xq = GetConfigFilter(structureItem);
        IntPtr configItemsIntPtr = NativeMethods.OdaServerApi._FindConfigItems(structureItem.GetIntPtr(), xq);
        return ReadList<StructureItem>(configItemsIntPtr);
    }

    public static IEnumerable<Host> FindHosts(this Connection connection)
    {
        if (connection.RemoteItem == null) { return Enumerable.Empty<Host>(); }

        IntPtr configItemsIntPtr = NativeMethods.OdaServerApi._Hosts(connection.GetIntPtr());
        return ReadList<Host>(configItemsIntPtr);
    }

    public static StructureItem FindItem(this Item item, string path)
    {
        if (item.RemoteItem == null) { return null; }

        IntPtr configItemIntPtr = NativeMethods.OdaServerApi._FindItem(item.GetIntPtr(), path);
        return oda.OdaOverride.ItemFactory.GetStorageItem(CreateByType(configItemIntPtr));
    }

    public static Domain FindDomain(this Item item, string path)
    {
        return FindItem(item, path) as Domain;
    }

    public static IEnumerable<Domain> FindDomains(this Domain domain)
    {
        var remoteDomain = domain?.RemoteItem as ODADomain;
        if (remoteDomain == null) { return Enumerable.Empty<Domain>(); }

        IntPtr configItemsIntPtr = NativeMethods.OdaServerApi._Domains(remoteDomain.GetIntPtr());
        return ReadList<Domain>(configItemsIntPtr);
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

    public static ODAItem GetItem(IntPtr _ptr, int index) => CreateByType(NativeMethods.OdaServerApi._GetItem(_ptr, index));
    public static ODAItem CreateByType(IntPtr item_ptr)
    {
        if (item_ptr == IntPtr.Zero)
            return null;
        ODAItem odaItem = null;
        switch (NativeMethods.OdaServerApi._GetType(item_ptr))
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

    public static IntPtr GetIntPtr(this Item item)
    {
        return item.RemoteItem?.GetIntPtr() ?? IntPtr.Zero;
    }
}