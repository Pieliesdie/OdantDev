using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using OdantDev;
using OdantDev.Model;

using OdantDevApp.Common;

namespace OdantDevApp.Model.ViewModels;

public partial class AddinSettings : ObservableObject
{
    private const string FILE_NAME = "AddinSettings.xml";
    private const string TEMPLATES_FOLDER_NAME = "Templates";

    private static readonly string templatePath = Path.Combine(VsixExtension.VSIXPath.FullName, TEMPLATES_FOLDER_NAME, FILE_NAME);
    private static readonly XmlSerializer serializer = new(typeof(AddinSettings));
    public static readonly string[] OdaLibraries = { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    [ObservableProperty] bool isVirtualizeTreeView;

    [ObservableProperty] AsyncObservableCollection<string>? pinnedItems;

    [ObservableProperty] AsyncObservableCollection<Project>? lastProjects;

    [ObservableProperty] bool forceUpdateReferences = true;

    [ObservableProperty] AsyncObservableCollection<string>? updateReferenceLibraries;

    [ObservableProperty] bool isSimpleTheme;

    [ObservableProperty] bool? isDarkTheme;

    [ObservableProperty] string? selectedDevelopeDomain;

    [ObservableProperty] AsyncObservableCollection<string>? lastOdaFolders;

    [ObservableProperty] AsyncObservableCollection<PathInfo>? odaFolders;

    [ObservableProperty] PathInfo selectedOdaFolder;

    [ObservableProperty] string? gitLabApiKey;

    [ObservableProperty] string? gitLabApiPath;
    [XmlIgnore] public string AddinSettingsPath { get; private set; }

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings = null;
        var path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            settings = LoadFromPath(path);
        }
        catch
        {
            try
            {
                settings = LoadFromPath(templatePath);         
                settings.AddinSettingsPath = path;  
                settings.Save();
            }
            catch { }
        }
        return settings ?? throw new Exception("Can't create settings file");
    }

    private static AddinSettings LoadFromPath(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var settings = (AddinSettings)serializer.Deserialize(fs);
        settings.OdaFolders.Remove(x => x.Name == "Last run");
        settings.OdaFolders?.Insert(0, new PathInfo("Last run", VsixExtension.LastOdaFolder.FullName));
        settings.AddinSettingsPath = path;
        return settings;
    }

    public bool Save()
    {
        try
        {
            if (AddinSettingsPath == null) return false;

            File.Delete(AddinSettingsPath);
            using var fs = new FileStream(AddinSettingsPath, FileMode.OpenOrCreate);
            serializer.Serialize(fs, this);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SaveAsync()
    {
        return await Task.Run(Save);
    }
}