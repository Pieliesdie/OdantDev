using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.Extensions.Caching.Memory;

using odaCore.Views;

using OdantDev;

using odaServer;

namespace oda.OdaOverride;

public static class ItemFactory
{
    static Connection _connection = null;
    static readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 500,
    });

    public static Connection Connection
    {
        get => _connection ??= new Connection(visible: true, login: false);
        set => _connection = value;
    }

    public static Class CreateClass(this StructureItem structureItem, string name)
    {
        var cls = (structureItem as Class) ?? structureItem.Class ?? throw new Exception("Class can't be created here");
        return cls.CreateChildClass(name);
    }

    public static Domain CreateDomain(this Domain domain, string name, string type)
    {
        if(domain.RemoteItem is not ODADomain remoteDomain)
        {
            throw new NullReferenceException("Can't get remote domain");
        }
        var newDomain = StructureItemEx.CreateByType(NativeMethods._Create_Domain(remoteDomain.GetIntPtr(), name, type));
        if (newDomain == null || !newDomain.Validate)
        {
            throw new Exception(newDomain.error);
        }
        return (GetStorageItem(newDomain) as Domain) ?? throw new Exception("Uknown error");
    }

    public static StructureItem GetStorageItem(ODAItem item)
    {
        if (item == null) { return null; }

        _cache.TryGetValue<StructureItem>(item.FullId, out var structureItem);
        if (structureItem != null && structureItem.IsDisposed)
        {
            _cache.Remove(structureItem);
            structureItem = null;
        }

        if (structureItem == null)
        {
            structureItem = CreateItem(item);
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1);
            _cache.Set(item.FullId, structureItem, cacheEntryOptions);
        }

        return structureItem;
    }

    private static StructureItem CreateItem(ODAItem item)
    {
        if (item == null) { return null; }
        StructureItem structureItem;
        Item item2 = item.Type != ODAItem.ItemType.HOST_ITEM ? GetStorageItem(item.Owner) : Connection;
        switch (item.Type)
        {
            case ODAItem.ItemType.DOMAIN_ITEM:
                structureItem = createDomain(item, item2);
                break;
            case ODAItem.ItemType.CLASS_ITEM:
                if (item2 != null && item.Owner != null && item.Owner.Id != item.Id && (item2.ItemType == ItemType.Solution || item2.ItemType == ItemType.ClassView))
                {
                    ClassView classView = (((item2.Host.AbilityExtension & ODAHost.AbilityExtensionFlags.LinkSupport) != ODAHost.AbilityExtensionFlags.LinkSupport) ? ((ClassView)new ClassViewClient(item2.Root.SelectSingleNode("LINKS/LINK[@id = '" + item.Id + "']") as xmlElement, (StructureItem)item2)) : ((ClassView)new ClassViewServer(item, item2)));
                    structureItem = classView;
                    if (!classView.Hide || classView.IsAdmin)
                    {
                        RegisterLink(structureItem, (StructureItem)item2);
                    }
                }
                else
                {
                    structureItem = new Class(item as ODAClass, item2);
                }
                break;
            case ODAItem.ItemType.HOST_ITEM:
                structureItem = new Host(item as ODAHost, Connection);
                break;
            default:
                throw new OdaException("item by id = '" + item.FullId + "' can't be creatig!"); ;

        }
        return structureItem;
    }
    private static Domain createDomain(ODAItem item, Item owner)
    {
        switch (item.SubType.ToLower())
        {
            case "base":
            case "organization":
                return new DomainOrganization(item as ODADomain, owner);
            case "developer":
                return new DomainDeveloper(item as ODADomain, owner);
            case "module":
                return new DomainModule(item as ODADomain, owner);
            case "workplace":
            case "solution":
                return new DomainSolution(item as ODADomain, owner);
            case "configuration":
                return new DomainConfiguration(item as ODADomain, owner);
            case "system":
                return new OdaOverride.DomainSystem(item as ODADomain, owner);
            case "executable":
                return new ExecutableDomain(item as ODADomain, owner);
            default:
                if (owner == null)
                {
                    return null;
                }

                if (owner is Host || (owner is Part && (owner as Part).ItemType == ItemType.RootPart && (item.Id == "DEVELOPE" || item.Id == "WORK")))
                {
                    return new Part(item as ODADomain, owner);
                }

                return new Domain(item as ODADomain, owner);
        }
    }
    private static bool TryGetItem(this Item context, string fullId, out Item item)
    {
        var reflectionParams = new object[] { fullId, null };
        var result = context.InvokePrivateMethod<bool>("TryGetItem", reflectionParams);
        item = (Item)reflectionParams[1];
        return result;
    }
    private static void UnregisterItem(this Item context, Item item)
    {
        context.InvokePrivateMethod("UnregisterItem", item);
    }
    private static void RegisterItem(this Item context, Item item)
    {
        context.InvokePrivateMethod("RegisterItem", item);
    }
    private static Item RegisterLink(Item _link, StructureItem owner)
    {
        if (_link == null)
        {
            return null;
        }

        if (owner.TryGetItem(_link.FullId, out var item))
        {
            if (!item.IsDisposed)
            {
                return item;
            }

            owner.UnregisterItem(item);
        }
        else
        {
            owner.RegisterItem(_link);
        }

        return _link;
    }
}
#if false // Decompilation log
'138' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'odaXML, Version=2.1.8060.20800, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'odaXML, Version=2.1.8060.20800, Culture=neutral, PublicKeyToken=null'
Load from: 'E:\git\OdantDev\Libraries\OdaLibs\odaXML.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
------------------
Resolve: 'odaShare, Version=2.1.8060.20799, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'odaShare, Version=2.1.8060.20799, Culture=neutral, PublicKeyToken=null'
Load from: 'E:\git\OdantDev\Libraries\OdaLibs\odaShare.dll'
------------------
Resolve: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Drawing.dll'
------------------
Resolve: 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll'
#endif
