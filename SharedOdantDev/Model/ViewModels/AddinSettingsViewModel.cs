using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace OdantDev.Model;

[ObservableObject]
public partial class AddinSettings
{
    private const string FILE_NAME = "AddinSettings.xml";
    private static readonly string TemplatePath = Path.Combine(Extension.VSIXPath.FullName, "Templates", FILE_NAME);
    private static readonly XmlSerializer Serializer = new(typeof(AddinSettings));

    public AddinSettings() { }

    [ObservableProperty]
    bool isVirtualizeTreeView;
        
    [ObservableProperty]
    ObservableCollection<Project> lastProjects;
    partial void OnLastProjectsChanging(ObservableCollection<Project> value)
    {
        value = new ObservableCollection<Project>(value.OrderByDescending(x => x.OpenTime).Take(15));
    }

    [ObservableProperty]
    bool forceUpdateReferences = true;

    [ObservableProperty]
    ObservableCollection<string> updateReferenceLibraries;

    [ObservableProperty]
    bool isSimpleTheme;

    [ObservableProperty]
    string selectedDevelopeDomain;

    [ObservableProperty]
    ObservableCollection<string> lastOdaFolders;

    [ObservableProperty]
    ObservableCollection<PathInfo> odaFolders;

    [ObservableProperty]
    PathInfo selectedOdaFolder;

    [ObservableProperty]
    string gitLabApiKey;

    [ObservableProperty]
    string gitLabApiPath;

    [XmlIgnore]
    public ObservableCollection<string> OdaLibraries => new() { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    [XmlIgnore]
    public string AddinSettingsPath { get; private set; }

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings;
        string path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            string loadPath = path;
            if (File.Exists(loadPath).Not())
            {
                loadPath = TemplatePath;
            }
            using var fs = new FileStream(loadPath, FileMode.Open, FileAccess.Read);
            settings = Serializer.Deserialize(fs) as AddinSettings;
            settings.OdaFolders.Remove(x => x.Name == "Last run");
        }
        catch
        {
            using var fs = new FileStream(TemplatePath, FileMode.Open, FileAccess.Read);
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
            using var fs = new FileStream(AddinSettingsPath, FileMode.OpenOrCreate);
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
        private ImageSource _icon;
        public Project(string name, string fullId, string domainName, DateTime openTime, BitmapImage icon = null)
        {
            Name = name;
            FullId = fullId;
            HostName = domainName;
            OpenTime = openTime;
            IconBase64 = icon.ToBase64String();
        }

        public string IconBase64 { get; set; }
        public ImageSource Icon => _icon ??= IconBase64?.FromBase64String();
        public bool HasIcon => Icon is not null;
        public string Name { get; set; }
        public string FullId { get; set; }
        public string HostName { get; set; }
        public DateTime OpenTime { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Project project &&
                   FullId == project.FullId;
        }

        public override int GetHashCode()
        {
            return -191063783 + EqualityComparer<string>.Default.GetHashCode(FullId);
        }
    }
}
