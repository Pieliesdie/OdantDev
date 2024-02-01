using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    public static readonly string[] OdaLibraries = ["odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll"];

    public delegate void ThemeChanged(ITheme theme);

    public event ThemeChanged OnThemeChanged;
    [ObservableProperty] bool isVirtualizeTreeView;
    [ObservableProperty] AsyncObservableCollection<string> pinnedItems = [];
    [ObservableProperty] int maxlastProjectsLength = 15;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    AsyncObservableCollection<RecentProject> lastProjects = [];
    partial void OnLastProjectsChanged(AsyncObservableCollection<RecentProject>? oldValue, AsyncObservableCollection<RecentProject>? value)
    {
        oldValue.CollectionChanged -= LastProjectsCollectionChanged;
        LastProjects.CollectionChanged += LastProjectsCollectionChanged;
    }

    private void LastProjectsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (LastProjects.Count > MaxlastProjectsLength - 1)
        {
            LastProjects = LastProjects.OrderByDescending(x => x.OpenTime).Take(MaxlastProjectsLength).ToAsyncObservableCollection();
        }
        OnPropertyChanged(nameof(FilteredLastProjects));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    string lastProjectsFilter = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    SortBy lastProjectsSortBy = SortBy.Date;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    SortOrder lastProjectsSortOrder = SortOrder.Descending;

    [XmlIgnore]
    public IEnumerable<RecentProject> FilteredLastProjects => GetFilteredLastProjects();
    private IEnumerable<RecentProject> GetFilteredLastProjects()
    {
        IEnumerable<RecentProject> filteredProjects = LastProjects;
        if (!string.IsNullOrEmpty(LastProjectsFilter))
        {
            try
            {
                filteredProjects = filteredProjects.Where(x => Regex.Match(x.Name, LastProjectsFilter, RegexOptions.IgnoreCase).Success);
            }
            catch { }
        }
        Func<RecentProject, IComparable> keySelector = GetKeySelector(LastProjectsSortBy);
        filteredProjects = ApplySorting(filteredProjects, keySelector, LastProjectsSortOrder);
        return filteredProjects;

        static IEnumerable<RecentProject> ApplySorting<T>(IEnumerable<RecentProject> projects, Func<RecentProject, T> keySelector, SortOrder sortOrder)
            => sortOrder == SortOrder.Ascending ? projects.OrderBy(keySelector) : projects.OrderByDescending(keySelector);

        static Func<RecentProject, IComparable> GetKeySelector(SortBy sortBy)
            => sortBy switch
            {
                SortBy.Date => x => x.OpenTime,
                SortBy.Name => x => x.Name,
                SortBy.HostName => x => x.HostName,
                _ => x => x.OpenTime
            };
    }

    [ObservableProperty] bool forceUpdateReferences = true;
    [ObservableProperty] AsyncObservableCollection<string> updateReferenceLibraries = [];
    [ObservableProperty] bool isSimpleTheme;
    [ObservableProperty] bool? mapToVisualStudioTheme = true;
    partial void OnMapToVisualStudioThemeChanging(bool? value)
    {
        if (value is false or null)
        {
            return;
        }

        bool val = VisualStudioIntegration.IsVisualStudioDark(VSCommon.EnvDTE.Instance);
        AppTheme.SetBaseTheme(val ? Theme.Dark : Theme.Light);
        OnAppThemeChanging(AppTheme);
    }
    [ObservableProperty] string? selectedDevelopeDomain;
    [ObservableProperty] AsyncObservableCollection<string> lastOdaFolders = [];
    [ObservableProperty] AsyncObservableCollection<PathInfo> odaFolders = [];
    [ObservableProperty] PathInfo selectedOdaFolder;
    [ObservableProperty] string? gitLabApiKey;
    [ObservableProperty] string? gitLabApiPath;
    [ObservableProperty] int gitlabTimeout = 15;
    [ObservableProperty] int structureItemTimeout = 10;
    [ObservableProperty] ThemeColors appTheme = ThemeColors.Default;
    partial void OnAppThemeChanging(ThemeColors value) => OnThemeChanged?.Invoke(value);
    [XmlIgnore] public string AddinSettingsPath { get; private set; }
    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings settings;
        var path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            settings = LoadFromPath(path);
        }
        catch
        {
            settings = LoadFromPath(TemplatePath);
            settings.AddinSettingsPath = path;
            //settings.Save();
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

        settings.AppTheme.PropertyChanged += (_, _) => settings.OnThemeChanged?.Invoke(settings.AppTheme);
        settings.OnMapToVisualStudioThemeChanging(settings.MapToVisualStudioTheme);
        settings.LastProjects.CollectionChanged += settings.LastProjectsCollectionChanged;
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

    public enum SortBy
    {
        [Description("Open date")]
        Date,
        [Description("Project name")]
        Name,
        [Description("Host name")]
        HostName
    }

    public enum SortOrder
    {
        Descending,
        Ascending
    }
}