using System.Reflection;
using odaServer;
using ItemFactory = oda.OdaOverride.ItemFactory;

namespace OdantDev;

public static class StructureItemEx
{
    private static HashSet<T> ReadList<T>(IntPtr intPtr)
    {
        if (intPtr == IntPtr.Zero)
        {
            return [];
        }
        var listLength = NativeMethods.OdaServerApi.GetLength(intPtr);
        var odaItems = Enumerable.Range(0, listLength)
            .Select(x => ItemFactory.GetStorageItem(CreateByType(NativeMethods.OdaServerApi.GetItem(intPtr, x))))
            .OfType<T>()
            .ToHashSet();
        NativeMethods.OdaServerApi.Release(intPtr);
        return odaItems;
    }

    public static IEnumerable<StructureItem> FindConfigItems(this StructureItem item)
    {
        if (item.RemoteItem == null || item.GetIntPtr() == IntPtr.Zero)
        {
            return [];
        }

        var xq = GetConfigFilter(item);
        var configItemsIntPtr = NativeMethods.OdaServerApi.FindConfigItems(item.GetIntPtr(), xq);
        var items = ReadList<StructureItem>(configItemsIntPtr);

        return items;
    }

    public static IEnumerable<Host> FindHosts(this Connection connection)
    {
        if (connection.RemoteItem == null)
        {
            return [];
        }

        var configItemsIntPtr = NativeMethods.OdaServerApi.Hosts(connection.GetIntPtr());
        return ReadList<Host>(configItemsIntPtr);
    }

    public static StructureItem? FindItem(this Item item, string path)
    {
        if (item.RemoteItem == null || item.GetIntPtr() == IntPtr.Zero)
        {
            return null;
        }

        var configItemIntPtr = NativeMethods.OdaServerApi.FindItem(item.GetIntPtr(), path);
        return ItemFactory.GetStorageItem(CreateByType(configItemIntPtr));
    }

    public static Domain? FindDomain(this Item item, string path)
    {
        return FindItem(item, path) as Domain;
    }

    public static IEnumerable<Domain> FindDomains(this Domain domain)
    {
        if (domain.RemoteItem is not ODADomain remoteDomain || remoteDomain.GetIntPtr() == IntPtr.Zero)
        {
            return [];
        }

        var configItemsIntPtr = NativeMethods.OdaServerApi.Domains(remoteDomain.GetIntPtr());
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
            return
                "./(C[@i='ROOT']/*[not(@t=('SOLUTION', 'WORKPLACE'))], *[not(@i = (../@i, '000000000000000', 'SYSTEM', 'DEVELOPE', 'WORK')) and not(@t=('SOLUTION', 'WORKPLACE'))])";
        }

        return "./(*[not(@i = ('000000000000000', 'SYSTEM')) and not(@t=('SOLUTION', 'WORKPLACE'))])";
    }

    public static ODAItem? GetItem(IntPtr ptr, int index) => CreateByType(NativeMethods.OdaServerApi.GetItem(ptr, index));

    public static ODAItem? CreateByType(IntPtr itemPtr)
    {
        if (itemPtr == IntPtr.Zero)
            return null;
        ODAItem odaItem = NativeMethods.OdaServerApi.GetType(itemPtr) switch
        {
            2 => new ODAHost(itemPtr),
            3 or 14 => new ODADomain(itemPtr),
            4 => new ODAClass(itemPtr),
            5 => new ODAFolder(itemPtr),
            6 => new ODAIndex(itemPtr),
            7 => new ODAObject(itemPtr),
            8 => new ODAPack(itemPtr),
            9 => new ODAAsyncResult(itemPtr),
            12 => new ODARole(itemPtr),
            13 => new ODAUser(itemPtr),
            _ => null
        };
        return odaItem;
    }

    public static IntPtr GetIntPtr(this ODAItem odaItem)
    {
        return (IntPtr)typeof(ODAItem)
            .GetField("_ptr", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(odaItem);
    }

    public static IntPtr GetIntPtr(this Item item)
    {
        return item.RemoteItem?.GetIntPtr() ?? IntPtr.Zero;
    }
}