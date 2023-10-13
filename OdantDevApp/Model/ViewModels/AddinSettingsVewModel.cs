using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

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
    public static readonly string[] OdaLibraries = new[] { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    public AddinSettings() { }

    [ObservableProperty] bool isVirtualizeTreeView;

    [ObservableProperty] AsyncObservableCollection<string> pinnedItems;

    [ObservableProperty] AsyncObservableCollection<Project> lastProjects;
    partial void OnLastProjectsChanging(AsyncObservableCollection<Project> value)
    {
        value = new AsyncObservableCollection<Project>(value.OrderByDescending(x => x.OpenTime).Take(15));
    }

    [ObservableProperty] bool forceUpdateReferences = true;

    [ObservableProperty] AsyncObservableCollection<string> updateReferenceLibraries;

    [ObservableProperty] bool isSimpleTheme;

    [ObservableProperty] string? selectedDevelopeDomain;

    [ObservableProperty] AsyncObservableCollection<string> lastOdaFolders;

    [ObservableProperty] AsyncObservableCollection<PathInfo> odaFolders;

    [ObservableProperty] PathInfo selectedOdaFolder;

    [ObservableProperty] string gitLabApiKey;

    [ObservableProperty] string gitLabApiPath;

    [XmlIgnore]
    public string AddinSettingsPath { get; private set; }

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings;
        var path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            string loadPath = path;
            if (File.Exists(loadPath).Not())
            {
                loadPath = templatePath;
            }
            using var fs = new FileStream(loadPath, FileMode.Open, FileAccess.Read);
            settings = serializer.Deserialize(fs) as AddinSettings;
            (settings?.OdaFolders).Remove(x => x.Name == "Last run");
        }
        catch
        {
            using var fs = new FileStream(templatePath, FileMode.Open, FileAccess.Read);
            settings = serializer.Deserialize(fs) as AddinSettings;
        }
        if(settings == null)
            throw new Exception("Can't create settings file");
        settings.AddinSettingsPath = path;
        settings.OdaFolders.Insert(0, new PathInfo("Last run", VsixExtension.LastOdaFolder.FullName));
        return settings;
    }

    public bool Save()
    {
        try
        {
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

    public struct Project(string name, string fullId, string domainName, DateTime openTime, BitmapSource? icon = null)
    {
        private ImageSource? icon;
        public string? IconBase64 { get; set; } = icon?.ToBase64String();
        public ImageSource? Icon => icon ??= IconBase64?.FromBase64String();
        public bool HasIcon => Icon is not null;
        public string Name { get; set; } = name;
        public string FullId { get; set; } = fullId;
        public string HostName { get; set; } = domainName;
        public DateTime OpenTime { get; set; } = openTime;

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
