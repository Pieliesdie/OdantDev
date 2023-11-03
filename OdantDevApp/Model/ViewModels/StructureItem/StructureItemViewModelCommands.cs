using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

using CommunityToolkit.Mvvm.Input;

using oda;

using OdantDev;

using SharedOdantDev.Common;

namespace OdantDevApp.Model.ViewModels;

public partial class StructureItemViewModel<T>
{
    [RelayCommand]
    public void Info()
    {
        if (Item == null) { return; }
        Clipboard.Clear();
        Clipboard.SetText(Item.FullId);
        logger?.Info($"FullId copied to clipboard!");
    }

    [RelayCommand]
    public async Task OpenDirAsync()
    {
        try
        {
            if (Item?.Dir is null)
            {
                logger?.Info($"{Item} has no directory");
                return;
            }

            var dirPath = await Task.Run(Item.Dir.RemoteFolder.LoadFolder).ConfigureAwait(true);

            if (Directory.Exists(dirPath).Not())
            {
                logger?.Info($"Folder {dirPath} doesn't exist for {Item}");
                return;
            }

            Process.Start("explorer", DevHelpers.ClearDomainAndClassInPath(dirPath));
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }

    [RelayCommand]
    public void Pin()
    {
        try
        {
            if (Item is null)
            {
                logger?.Info("Can't pin this item");
                return;
            }
            if (!IsPinned)
            {
                var copy = new StructureItemViewModel<StructureItem>(Item, Parent as StructureItemViewModel<StructureItem>, logger, connection);
                if (copy.Item?.FullId is null || connection is null)
                    return;
                connection.PinnedItems.Add(copy);
                connection.AddinSettings.PinnedItems?.Add(copy.Item.FullId);

            }
            else
            {
                if (connection is null)
                    return;
                connection.PinnedItems.Remove(x => x.Item?.FullId == Item.FullId);
                connection.AddinSettings.PinnedItems?.Remove(Item.FullId);
            }
            _ = connection.AddinSettings.SaveAsync();
            OnPropertyChanged(nameof(IsPinned));
        }
        catch (Exception ex)
        {
            logger?.Info(ex.ToString());
        }
    }
}
