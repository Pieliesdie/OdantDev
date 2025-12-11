using System;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using odaCore.Views;

using OdantDev;

using odaServer;

namespace oda.OdaOverride;


public static class ItemFactory
{
    private static readonly MemoryCache cache = new(new MemoryCacheOptions());

    private static readonly MemoryCacheEntryOptions defaultCacheOptions = new MemoryCacheEntryOptions()
        .SetSize(1)
        .SetPriority(CacheItemPriority.NeverRemove);

    public static Connection? Connection { get => field ??= CommonEx.Connection; set; }

    public static Class? CreateClassByXml(this StructureItem item, string xml)
    {
        var cls = item as Class ?? item.Class ?? throw new Exception("Class can't be created here");
        var newItem = (cls.RemoteItem as ODAClass)?.CreateClassByXML(xml);
        return GetStorageItem(newItem) as Class;
    }

    public static Class? CreateClass(this StructureItem item, string name)
    {
        name = name.Replace('"', '\'');
        var newItemLink = item.ExecuteCommand($"create_class?name={name}");
        var newItem = item.RemoteItem?.FindItem(newItemLink);
        return GetStorageItem(newItem) as Class;
    }

    public static Domain? CreateDomainByXml(this StructureItem item, string xml)
    {
        var newItemLink = item.ExecuteCommand("create_domain", xml);
        if (string.IsNullOrEmpty(newItemLink)) { return null; }
        var newItem = item.RemoteItem?.FindItem(newItemLink);
        return GetStorageItem(newItem) as Domain;
    }

    private static string ExecuteCommand(this StructureItem structureItem, string cmd, string? parameter = null)
    {
        var result = parameter is null
            ? structureItem.RemoteItem.Command(cmd)
            : structureItem.RemoteItem.Command(cmd, parameter);

        var error = structureItem.RemoteItem.error;
        if (!string.IsNullOrEmpty(error))
        {
            throw new InvalidOperationException(error);
        }
        if (result.Contains("~Error~"))
        {
            throw new InvalidOperationException(result);
        }

        return result;
    }

    public static Domain? CreateDomain(this Domain domain, string name, string type)
    {
        if (domain.RemoteItem is not ODADomain remoteDomain)
        {
            throw new NullReferenceException("Can't get remote domain");
        }
        var newDomain = StructureItemEx.CreateByType(NativeMethods.OdaServerApi.Create_Domain(remoteDomain.GetIntPtr(), name, type));
        if (newDomain == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(newDomain.error).Not() || !newDomain.Validate)
        {
            throw new Exception(newDomain.error);
        }
        return GetStorageItem(newDomain) as Domain ?? throw new Exception("Unknown error");
    }

    public static StructureItem? GetStorageItem(ODAItem? item)
    {
        if (item == null) { return null; }

        var structureItem = cache.GetOrCreate(item.FullId, (entry) => 
            {
                entry.SetOptions(defaultCacheOptions);
                return CreateItem(item);
            }
        );
        if (structureItem is not { IsDisposed: true }) 
            return structureItem;

        cache.Remove(structureItem);
        structureItem = CreateItem(item);
        cache.Set(item.FullId, structureItem, defaultCacheOptions);

        return structureItem;
    }

    private static StructureItem? CreateItem(ODAItem? item)
    {
        if (item == null)
        {
            return null;
        }

        StructureItem structureItem;
        Item item2 = item.Type != ODAItem.ItemType.HOST_ITEM ? GetStorageItem(item.Owner) : Connection;
        switch (item.Type)
        {
            case ODAItem.ItemType.DOMAIN_ITEM:
                structureItem = CreateDomain(item, item2);
                break;
            case ODAItem.ItemType.CLASS_ITEM:
                if (item2 != null && item.Owner != null && item.Owner.Id != item.Id && (item2.ItemType == ItemType.Solution || item2.ItemType == ItemType.ClassView))
                {
                    ClassView classView = (item2.Host.AbilityExtension & ODAHost.AbilityExtensionFlags.LinkSupport) != ODAHost.AbilityExtensionFlags.LinkSupport 
                        ? new ClassViewClient(item2.Root.SelectSingleNode("LINKS/LINK[@id = '" + item.Id + "']") as xmlElement, (StructureItem)item2) 
                        : new ClassViewServer(item, item2);
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
    private static Domain CreateDomain(ODAItem item, Item owner)
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

                if (owner is Host || (owner is Part && (owner as Part)?.ItemType == ItemType.RootPart && (item.Id == "DEVELOPE" || item.Id == "WORK")))
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
    private static Item RegisterLink(Item link, StructureItem owner)
    {
        if (link == null)
        {
            return null;
        }

        if (owner.TryGetItem(link.FullId, out var item))
        {
            if (!item.IsDisposed)
            {
                return item;
            }

            owner.UnregisterItem(item);
        }
        else
        {
            owner.RegisterItem(link);
        }

        return link;
    }
}
