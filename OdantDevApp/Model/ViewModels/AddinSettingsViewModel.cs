﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

using OdantDevApp.Common;

namespace OdantDev.Model;

public partial class AddinSettings : ObservableObject
{
    private const string FILE_NAME = "AddinSettings.xml";
    private const string TEMPLATES_FOLDER_NAME = "Templates";

    private static readonly string TemplatePath = Path.Combine(VsixExtension.VSIXPath.FullName, TEMPLATES_FOLDER_NAME, FILE_NAME);
    private static readonly XmlSerializer Serializer = new(typeof(AddinSettings));
    public static readonly string[] OdaLibraries = new[] { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    public AddinSettings() { }

    [ObservableProperty]
    bool isVirtualizeTreeView;

    [ObservableProperty]
    AsyncObservableCollection<string> pinnedItems;

    [ObservableProperty]
    AsyncObservableCollection<Project> lastProjects;
    partial void OnLastProjectsChanging(AsyncObservableCollection<Project> value)
    {
        value = new AsyncObservableCollection<Project>(value.OrderByDescending(x => x.OpenTime).Take(15));
    }

    [ObservableProperty]
    bool forceUpdateReferences = true;

    [ObservableProperty]
    AsyncObservableCollection<string> updateReferenceLibraries;

    //[ObservableProperty]
    //bool isDarkTheme;

    [ObservableProperty]
    bool isSimpleTheme;

    [ObservableProperty]
    string selectedDevelopeDomain;

    [ObservableProperty]
    AsyncObservableCollection<string> lastOdaFolders;

    [ObservableProperty]
    AsyncObservableCollection<PathInfo> odaFolders;

    [ObservableProperty]
    PathInfo selectedOdaFolder;

    [ObservableProperty]
    string gitLabApiKey;

    [ObservableProperty]
    string gitLabApiPath;

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
            settings?.OdaFolders.Remove(x => x.Name == "Last run");
        }
        catch
        {
            using var fs = new FileStream(TemplatePath, FileMode.Open, FileAccess.Read);
            settings = Serializer.Deserialize(fs) as AddinSettings;
        }
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
            Serializer.Serialize(fs, this);
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

    public struct Project
    {
        private ImageSource _icon;
        public Project(string name, string fullId, string domainName, DateTime openTime, BitmapSource icon = null)
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
