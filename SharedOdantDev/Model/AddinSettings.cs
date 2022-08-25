using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace OdantDev.Model;
public class AddinSettings : INotifyPropertyChanged
{
    private const string FILE_NAME = "AddinSettings.xml";
    private static readonly string TemplatePath = Path.Combine(Extension.VSIXPath.FullName, @"Templates\AddinSettings.xml");
    private static readonly XmlSerializer Serializer = new(typeof(AddinSettings));
    private ObservableCollection<PathInfo> odaFolders;
    private bool isLazyTreeLoad;
    private bool isSimpleTheme;
    private ObservableCollection<Project> lastProjects;
    private ObservableCollection<string> odaLibraries = new ObservableCollection<string> { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };
    private ObservableCollection<string> updateReferenceLibraries;
    private string selectedDevelopeDomain;
    private ObservableCollection<string> lastOdaFolders;
    private PathInfo selectedOdaFolder;

    public AddinSettings() { }

    public event PropertyChangedEventHandler PropertyChanged;

    [XmlIgnore]
    public ObservableCollection<string> OdaLibraries
    {
        get => odaLibraries;
        set
        {
            odaLibraries = value;
            NotifyPropertyChanged("OdaLibraries");
        }
    }

    public ObservableCollection<Project> LastProjects
    {
        get => lastProjects;
        set
        {
            lastProjects = new ObservableCollection<Project>(value.OrderByDescending(x => x.OpenTime));
            NotifyPropertyChanged("LastProjects");
        }
    }

    public ObservableCollection<string> UpdateReferenceLibraries
    {
        get => updateReferenceLibraries;
        set
        {
            updateReferenceLibraries = value;
            NotifyPropertyChanged("UpdateReferenceLibraries");
        }
    }

    public bool IsSimpleTheme
    {
        get => isSimpleTheme;
        set
        {
            isSimpleTheme = value;
            NotifyPropertyChanged("IsSimpleTheme");
        }
    }

    public bool IsLazyTreeLoad
    {
        get => isLazyTreeLoad;
        set
        {
            isLazyTreeLoad = value;
            NotifyPropertyChanged("IsLazyTreeLoad");
        }
    }

    public string SelectedDevelopeDomain
    {
        get => selectedDevelopeDomain;
        set
        {
            selectedDevelopeDomain = value;
            NotifyPropertyChanged("SelectedDevelopeDomain");
        }
    }

    public ObservableCollection<string> LastOdaFolders
    {
        get => lastOdaFolders;
        set
        {
            lastOdaFolders = value;
            NotifyPropertyChanged("LastOdaFolders");
        }
    }

    public ObservableCollection<PathInfo> OdaFolders
    {
        get => odaFolders;
        set
        {
            odaFolders = value;
            NotifyPropertyChanged("OdaFolders");
        }
    }

    public PathInfo SelectedOdaFolder
    {
        get => selectedOdaFolder;
        set
        {
            selectedOdaFolder = value;
            NotifyPropertyChanged("SelectedOdaFolder");
        }
    }
    [XmlIgnore]
    public string AddinSettingsPath { get; private set;}                  

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings = null;
        var path = Path.Combine(folder.FullName, FILE_NAME);

        try
        {
            if (File.Exists(path).Not())
            {
                path = TemplatePath;
            }
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
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

    private void NotifyPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
