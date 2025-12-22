using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using oda.OdaOverride;
using OdantDevApp.Common;
using File = System.IO.File;
using VisualStudioIntegration = OdantDevApp.VSCommon.VisualStudioIntegration;

namespace OdantDevApp.Model.ViewModels.Settings;

public partial class AddinSettings : ObservableObject
{
    private const string FILE_NAME = "AddinSettings.xml";
    private const string TEMPLATES_FOLDER_NAME = "Templates";
    private static string TemplatePath => Path.Combine(VsixEx.VsixPath.FullName, TEMPLATES_FOLDER_NAME, FILE_NAME);
    private static XmlSerializer Serializer => new(typeof(AddinSettings));

    private AddinSettings() { }

    public static AddinSettings Instance { get; set; } = Create(CommonEx.DefaultSettingsFolder);

    public static readonly string[] OdaLibraries =
        ["odaMain.dll", "odaShare.dll", "odaLib.dll", "odaXML.dll", "odaCore.dll"];

    public delegate void ThemeChanged(ITheme theme);

    public event ThemeChanged? OnThemeChanged;

    [ObservableProperty]
    public partial bool IsVirtualizeTreeView { get; set; }
    
    [ObservableProperty] 
    public partial AsyncObservableCollection<string> PinnedItems { get; set; } = [];
    
    [ObservableProperty] 
    public partial int MaxlastProjectsLength { get; set; } = 15;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    public partial AsyncObservableCollection<RecentProject> LastProjects { get; set; } = [];

    partial void OnLastProjectsChanged(
        AsyncObservableCollection<RecentProject>? oldValue,
        AsyncObservableCollection<RecentProject>? value
    )
    {
        oldValue?.CollectionChanged -= LastProjectsCollectionChanged;
        LastProjects.CollectionChanged += LastProjectsCollectionChanged;
    }

    private void LastProjectsCollectionChanged(object sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (LastProjects.Count > MaxlastProjectsLength - 1)
        {
            LastProjects = LastProjects
                .OrderByDescending(x => x.OpenTime)
                .Take(MaxlastProjectsLength)
                .ToAsyncObservableCollection();
        }

        OnPropertyChanged(nameof(FilteredLastProjects));
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    public partial string LastProjectsFilter { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    public partial SortBy LastProjectsSortBy { get; set; } = SortBy.Date;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredLastProjects))]
    public partial SortOrder LastProjectsSortOrder { get; set; } = SortOrder.Descending;

    [XmlIgnore] 
    public IEnumerable<RecentProject> FilteredLastProjects => GetFilteredLastProjects();

    private IEnumerable<RecentProject> GetFilteredLastProjects()
    {
        IEnumerable<RecentProject> filteredProjects = LastProjects;
        if (!string.IsNullOrEmpty(LastProjectsFilter))
        {
            filteredProjects = filteredProjects
                .Where(x => Regex.Match(x.Name, LastProjectsFilter, RegexOptions.IgnoreCase).Success);
        }

        var keySelector = GetKeySelector(LastProjectsSortBy);
        filteredProjects = ApplySorting(filteredProjects, keySelector, LastProjectsSortOrder);
        return filteredProjects;

        static IEnumerable<RecentProject> ApplySorting<T>(IEnumerable<RecentProject> projects,
            Func<RecentProject, T> keySelector, SortOrder sortOrder)
            => sortOrder == SortOrder.Ascending
                ? projects.OrderBy(keySelector)
                : projects.OrderByDescending(keySelector);

        static Func<RecentProject, IComparable> GetKeySelector(SortBy sortBy)
            => sortBy switch
            {
                SortBy.Date => x => x.OpenTime,
                SortBy.Name => x => x.Name,
                SortBy.HostName => x => x.HostName,
                _ => x => x.OpenTime
            };
    }

    [ObservableProperty]
    public partial bool ForceUpdateReferences { get; set; } = true;

    [ObservableProperty]
    public partial AsyncObservableCollection<string> UpdateReferenceLibraries { get; set; } = [];

    [ObservableProperty]
    public partial bool IsSimpleTheme { get; set; }

    [ObservableProperty]
    public partial bool? MapToVisualStudioTheme { get; set; } = true;

    partial void OnMapToVisualStudioThemeChanging(bool? value)
    {
        if (value is false or null)
        {
            return;
        }

        var val = VisualStudioIntegration.IsDarkTheme(VSCommon.EnvDTE.Instance);
        AppTheme.SetBaseTheme(val ? Theme.Dark : Theme.Light);
        OnAppThemeChanging(AppTheme);
    }

    [ObservableProperty] 
    public partial string? SelectedDevelopeDomain { get; set; }
    
    [ObservableProperty] 
    public partial AsyncObservableCollection<string> LastOdaFolders { get; set; } = [];
    
    [ObservableProperty] 
    public partial AsyncObservableCollection<PathInfo> OdaFolders { get; set; } = [];
    
    [ObservableProperty] 
    public partial PathInfo SelectedOdaFolder { get; set; }
    
    [ObservableProperty] 
    public partial string? GitLabApiKey { get; set; }
    
    [ObservableProperty] 
    public partial string? GitLabApiPath { get; set; }
    
    [ObservableProperty] 
    public partial int GitlabTimeout { get; set; } = 15;
    
    [ObservableProperty] 
    public partial int StructureItemTimeout { get; set; } = 10;
   
    [ObservableProperty] 
    public partial ThemeColors AppTheme { get; set; } = ThemeColors.Default;

    partial void OnAppThemeChanging(ThemeColors value) => OnThemeChanged?.Invoke(value);
    [XmlIgnore] public string? AddinSettingsPath { get; private set; }

    public static AddinSettings Create(DirectoryInfo folder)
    {
        AddinSettings? settings;
        var path = Path.Combine(folder.FullName, FILE_NAME);
        try
        {
            settings = LoadFromPath(path);
        }
        catch
        {
            settings = LoadFromPath(TemplatePath);
            settings.AddinSettingsPath = path;
        }

        return settings ?? throw new Exception($"Can't load settings file {path}");
    }

    private static AddinSettings LoadFromPath(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
        var settings = (AddinSettings)Serializer.Deserialize(fs);
        settings.OdaFolders.Remove(x => x.Name == "Last run");
        if (VsixEx.LastOdaFolder?.FullName is { } lastOdaFolder)
        {
            settings.OdaFolders?.Insert(0, new PathInfo("Last run", lastOdaFolder));
        }
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

    [RelayCommand]
    public void Open()
    {
        if (AddinSettingsPath != null)
        {
            Process.Start(AddinSettingsPath);
        }
    }

    public enum SortBy
    {
        [Description("Open date")] Date,
        [Description("Project name")] Name,
        [Description("Host name")] HostName
    }

    public enum SortOrder
    {
        Descending,
        Ascending
    }
}