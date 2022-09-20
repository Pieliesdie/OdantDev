using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace OdantDev.Model;

public partial class AddinSettings : ObservableObject
{
    private const string FILE_NAME = "AddinSettings.xml";
    private static readonly string TemplatePath = Path.Combine(Extension.VSIXPath.FullName, "Templates", FILE_NAME);
    private static readonly XmlSerializer Serializer = new(typeof(AddinSettings));

    public AddinSettings() { }

    [ObservableProperty]
    private ObservableCollection<Project> lastProjects;
    partial void OnLastProjectsChanging(ObservableCollection<Project> value)
    {
        value = new ObservableCollection<Project>(value.OrderByDescending(x => x.OpenTime).Take(15));
    }

    [ObservableProperty]
    private bool forceUpdateReferences = true;

    [XmlIgnore]
    [ObservableProperty]
    private ObservableCollection<string> odaLibraries = new ObservableCollection<string> { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    [ObservableProperty]
    private ObservableCollection<string> updateReferenceLibraries;

    [ObservableProperty]
    private bool isSimpleTheme;

    [ObservableProperty]
    private bool isLazyTreeLoad;

    [ObservableProperty]
    private string selectedDevelopeDomain;

    [ObservableProperty]
    private ObservableCollection<string> lastOdaFolders;

    [ObservableProperty]
    private ObservableCollection<PathInfo> odaFolders;

    [ObservableProperty]
    private PathInfo selectedOdaFolder;

    [XmlIgnore]
    public string AddinSettingsPath { get; private set; }

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings = null;
        var path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            var loadPath = path;
            if (File.Exists(loadPath).Not())
            {
                loadPath = TemplatePath;
            }
            using FileStream fs = new FileStream(loadPath, FileMode.Open, FileAccess.Read);
            settings = Serializer.Deserialize(fs) as AddinSettings;
            settings.OdaFolders.Remove(x => x.Name == "Last run");
        }
        catch
        {
            using FileStream fs = new FileStream(TemplatePath, FileMode.Open, FileAccess.Read);
            settings = Serializer.Deserialize(fs) as AddinSettings;
        }
        settings.AddinSettingsPath = path;
        settings.OdaFolders.Insert(0, new PathInfo("Last run", Extension.LastOdaFolder.FullName));
        return settings;
    }

    public bool Save()
    {
        try
        {
            File.Delete(AddinSettingsPath);
            using FileStream fs = new FileStream(AddinSettingsPath, FileMode.OpenOrCreate);
            Serializer.Serialize(fs, this);
            return true;
        }
        catch
        {
            return false;
        }
    }


    public struct Project
    {
        public Project(string name, string description, string fullId, string domainName, DateTime openTime)
        {
            Name = name;
            Description = description;
            FullId = fullId;
            HostName = domainName;
            OpenTime = openTime;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string FullId { get; set; }
        public string HostName { get; set; }
        public DateTime OpenTime { get; set; }
    }
}
