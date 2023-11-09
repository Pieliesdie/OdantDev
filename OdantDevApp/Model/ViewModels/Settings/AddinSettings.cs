using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

using MaterialDesignThemes.Wpf;

using oda.OdaOverride;

using OdantDev;
using OdantDev.Model;

using OdantDevApp.Common;

namespace OdantDevApp.Model.ViewModels.Settings;

public partial class AddinSettings : ObservableObject
{
    private const string FILE_NAME = "AddinSettings.xml";
    private const string TEMPLATES_FOLDER_NAME = "Templates";
    private static string TemplatePath => Path.Combine(VsixExtension.VSIXPath.FullName, TEMPLATES_FOLDER_NAME, FILE_NAME);
    private static XmlSerializer Serializer => new(typeof(AddinSettings));
    private AddinSettings() { }
    public static AddinSettings Instance { get; set; } = Create(CommonEx.DefaultSettingsFolder);
    public static readonly string[] OdaLibraries = { "odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll" };

    public delegate void ThemeChanged(ITheme theme);
    public event ThemeChanged OnThemeChanged;

    [ObservableProperty] bool isVirtualizeTreeView;

    [ObservableProperty] AsyncObservableCollection<string>? pinnedItems;

    [ObservableProperty] AsyncObservableCollection<RecentProject>? lastProjects;

    [ObservableProperty] bool forceUpdateReferences = true;

    [ObservableProperty] AsyncObservableCollection<string>? updateReferenceLibraries;

    [ObservableProperty] bool isSimpleTheme;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AppTheme))]
    bool? isDarkTheme;
    partial void OnIsDarkThemeChanging(bool? value)
    {
        bool val = value ?? VisualStudioIntegration.IsVisualStudioDark(VSCommon.EnvDTE.Instance);
        AppTheme.SetBaseTheme(val ? Theme.Dark : Theme.Light);
        OnThemeChanged?.Invoke(AppTheme);
    }

    [ObservableProperty] string? selectedDevelopeDomain;

    [ObservableProperty] AsyncObservableCollection<string>? lastOdaFolders;

    [ObservableProperty] AsyncObservableCollection<PathInfo>? odaFolders;

    [ObservableProperty] PathInfo selectedOdaFolder;

    [ObservableProperty] string? gitLabApiKey;

    [ObservableProperty] string? gitLabApiPath;

    [ObservableProperty] int gitlabTimeout = 15;

    [ObservableProperty] int structureItemTimeout = 10;

    [ObservableProperty] ThemeColors appTheme = ThemeColors.Default;

    partial void OnAppThemeChanging(ThemeColors value)
    {
        OnThemeChanged?.Invoke(value);
    }

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
                settings = LoadFromPath(TemplatePath);
                settings.AddinSettingsPath = path;
                settings.Save();
            }
            catch { }
        }
        return settings ?? throw new Exception($"Can't load settings file {path}");
    }

    private static AddinSettings LoadFromPath(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var settings = (AddinSettings)Serializer.Deserialize(fs);
        settings.OdaFolders.Remove(x => x.Name == "Last run");
        settings.OdaFolders?.Insert(0, new PathInfo("Last run", VsixExtension.LastOdaFolder.FullName));
        settings.AddinSettingsPath = path;
        if (settings.OdaFolders?.FirstOrDefault() is { } pathInfo
            && string.IsNullOrEmpty(settings.SelectedOdaFolder.Path)
            && !string.IsNullOrEmpty(pathInfo.Path))
        {
            settings.SelectedOdaFolder = pathInfo;
        }

        settings.OnIsDarkThemeChanging(settings.IsDarkTheme);
        settings.AppTheme.PropertyChanged += (_, _) => settings.OnThemeChanged?.Invoke(settings.AppTheme);
        return settings;
    }

    public bool Save()
    {
        try
        {
            if (AddinSettingsPath == null) return false;

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
}