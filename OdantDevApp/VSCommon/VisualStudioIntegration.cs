using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using OdantDev.Model;
using OdantDevApp.Model.ViewModels.Settings;
using OdantDevApp.VSCommon.ProjectStrategies;
using SharedOdantDevLib.WinApi;
using VSLangProj;
using VSLangProj80;
using File = System.IO.File;
using Task = System.Threading.Tasks.Task;

namespace OdantDevApp.VSCommon;

public sealed partial class VisualStudioIntegration
{
    #region Private Variables

    private bool IsLastBuildSuccess { get; set; } = true;
    private bool FireEvents { get; set; } = false;
    private BuildEvents? BuildEvents { get; set; }
    private SolutionEvents? SolutionEvents { get; set; }
    private DTE2 EnvDte { get; }
    private ConcurrentDictionary<string, BuildInfo> LoadedModules { get; } = new();
    private AddinSettings AddinSettings { get; }
    private DirectoryInfo OdaFolder => new(AddinSettings.SelectedOdaFolder.Path);
    private ILogger? Logger { get; }
    private readonly IReadOnlyCollection<IProjectStrategy> projectStrategies;

    private Project[] ActiveSolutionProjects =>
        ((EnvDte.ActiveSolutionProjects as object[]) ?? []).Cast<Project>().ToArray();

    #endregion

    public VisualStudioIntegration(AddinSettings addinSettings, DTE2 dte, ILogger? logger = null)
    {
        Logger = logger;
        AddinSettings = addinSettings;
        projectStrategies =
        [
            new SdkProjectStrategy(logger),
            new LegacyProjectStrategy(logger)
        ];
#if DEBUG
        EnvDte = dte;
        return;
#else
        EnvDte = dte
                 ?? throw new NullReferenceException("Can't get EnvDTE2 from visual studio");
#endif
        try
        {
            using var retryComCallsfilter = OleMessageFilter.MessageFilterRegister();
            if (EnvDte.Solution.IsOpen)
            {
                EnvDte.Solution.Close();
            }
        }
        catch (Exception e)
        {
            Logger?.Error($"Error while initialize VisualStudioIntegration: {e}");
        }
    }

    private IProjectStrategy GetStrategy(Project project)
    {
        return projectStrategies.First(s => s.IsMatch(project));
    }

    public static bool IsDarkTheme(DTE2? dte)
    {
        try
        {
            if (dte == null) return false;
            var uintClr = dte.GetThemeColor(vsThemeColors.vsThemeColorToolWindowBackground);
            var bytes = BitConverter.GetBytes(uintClr);
            var defaultBackground = Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
            var isDarkTheme = (384 - defaultBackground.R - defaultBackground.G - defaultBackground.B) > 0;
            return isDarkTheme;
        }
        catch
        {
            return false;
        }
    }

    #region Visual studio events

    private void SubscribeToStudioEvents()
    {
        FireEvents = true;
        if (SolutionEvents is null)
        {
            SolutionEvents = EnvDte.Events.SolutionEvents;
            if (SolutionEvents != null)
            {
                SolutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;
                SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;
            }
            else
            {
                Logger?.Info("Can't initialize EnvDTE.Events.SolutionEvents");
            }
        }

        if (BuildEvents is null)
        {
            BuildEvents = EnvDte.Events.BuildEvents;
            if (BuildEvents != null)
            {
                BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
                BuildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;
            }
            else
            {
                Logger?.Info("Can't initialize EnvDTE.Events.BuildEvents");
            }
        }
    }

    private void UnsubscribeToStudioEvents()
    {
        FireEvents = false;
    }

    // Если закрыть Solution и потом открыть модуль он попадает в коллекцию, после этого открывается Solution и событие очищает коллекцию модулей
    // добавил эвент SolutionEvents_AfterClosing, думаю в таком случае будет правильнее там очищать
    private void SolutionEvents_AfterClosing()
    {
        if (!FireEvents) return;
        LoadedModules.Clear();
        oda.OdaOverride.INI.DebugINI.Clear();
        oda.OdaOverride.INI.DebugINI.Save();
        UnsubscribeToStudioEvents();
    }

    private void SolutionEvents_ProjectRemoved(Project project)
    {
        if (!FireEvents) return;
        var guid = GetProjectGuid(project);
        LoadedModules.TryRemove(guid, out _);
    }

    private void BuildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform,
        string solutionConfig, bool success)
    {
        if (!FireEvents) return;
        IsLastBuildSuccess = success;
    }

    private void BuildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
    {
        if (!FireEvents) return;
        if ((action != vsBuildAction.vsBuildActionBuild && action != vsBuildAction.vsBuildActionRebuildAll))
        {
            return;
        }

        if (!IsLastBuildSuccess)
        {
            return;
        }

        foreach (var project in ActiveSolutionProjects)
        {
            if (!LoadedModules.TryGetValue(GetProjectGuid(project), out _))
            {
                continue;
            }

            CopyToOdaBin(project);
        }
    }

    private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
    {
        if (!FireEvents) return;
        if (action != vsBuildAction.vsBuildActionBuild && action != vsBuildAction.vsBuildActionRebuildAll)
        {
            return;
        }

        foreach (var project in ActiveSolutionProjects)
        {
            if (!LoadedModules.TryGetValue(GetProjectGuid(project), out _))
            {
                continue;
            }

            var strategy = GetStrategy(project);
            if (!strategy.IncreaseVersion(project))
            {
                EnvDte.ExecuteCommand("Build.Cancel");
                return;
            }
        }
    }

    #endregion

    #region Common Methods for Project item

    private bool CopyToOdaBin(Project project)
    {
        try
        {
            var projectStrategy = GetStrategy(project);
            var version = projectStrategy.GetVersion(project);

            var outDirParent = new FileInfo(project.FullName).Directory?.Parent
                               ?? throw new DirectoryNotFoundException(project.FullName);

            outDirParent.CreateSubdirectory("bin").TryDeleteDirectory();
            var outputDir = outDirParent.CreateSubdirectory("bin");
            var versionPath = @$"{version.Major}.{version.Minor}\{version.Build}\{version.Revision}";
            var outputBinDir = outputDir.CreateSubdirectory(versionPath);
            var moduleDir = new ModuleDir(project);
            var clientBinDir = OdaFolder.CreateSubdirectory(Path.Combine("bin", versionPath, moduleDir.Name));

            moduleDir.BinInfo.CopyToDir(clientBinDir);
            moduleDir.BinInfo.CopyToDir(outputBinDir);
            moduleDir.PdbInfo.CopyToDir(outputBinDir);

            if (!LoadedModules.TryGetValue(GetProjectGuid(project), out var buildInfo))
            {
                return false;
            }

            var remoteBinDir = buildInfo.RemoteDir.Class.Dir.OpenOrCreateFolder("bin");
            var remoteVersionDir = remoteBinDir.OpenOrCreateFolder(versionPath);

            if ((remoteVersionDir.SaveFile(moduleDir.BinInfo.FullName) &&
                 remoteVersionDir.SaveFile(moduleDir.PdbInfo.FullName)).Not())
            {
                throw new OdaException("Can't copy bin to server", remoteVersionDir);
            }

            if (moduleDir.Refs.Any())
            {
                var refDir = outputDir.CreateSubdirectory("ref");
                var remoteRefDir = remoteBinDir.OpenOrCreateFolder("ref");
                var clientRefDir = OdaFolder.CreateSubdirectory(Path.Combine("bin", "ref"));
                moduleDir.Refs.ToList().ForEach(x =>
                {
                    x.CopyToDir(refDir);
                    if (x is not FileInfo || x.Extension != ".dll") return;

                    var isValidVersion = Version.TryParse(FileVersionInfo.GetVersionInfo(x.FullName).FileVersion,
                        out var fileVersion);
                    x.CopyToDir(isValidVersion
                        ? clientRefDir.CreateSubdirectory(fileVersion.ToString())
                        : clientRefDir);
                    remoteRefDir.SaveFile(x.FullName);
                });
            }

            CopyModuleDirToServer(buildInfo);
        }
        catch (Exception ex)
        {
            Logger?.Error($"Can't copy files to odant 'Bin' folder: {ex.Message}");
            return false;
        }

        return true;
    }

    private bool CopyModuleDirToServer(BuildInfo buildInfo)
    {
        try
        {
            var isCopied = buildInfo.RemoteDir.Class.Dir
                .OpenOrCreateFolder("modules")
                .FolderToServer(buildInfo.LocalDir.CreateSubdirectory("modules"));
            var isSaved = buildInfo.RemoteDir.Class.Save();
            buildInfo.RemoteDir.Class.Dir.Reset();
            return isCopied && isSaved;
        }
        catch (Exception e)
        {
            Logger?.Error($"Can't copy 'modules' to server: {e.Message}");
            return false;
        }
    }

    private static string GetProjectGuid(Project project)
    {
        return project.UniqueName;
    }

    #endregion

    #region Download and init  logic for Module

    public async Task<bool> OpenModuleAsync(StructureItem item)
    {
        var result = false;
        var staThread = new System.Threading.Thread(() => result = OpenModule(item));
        staThread.SetApartmentState(ApartmentState.STA); //COM needs STA
        staThread.Start();
        await Task.Run(staThread.Join);
        return result;
    }

    private bool OpenModule(StructureItem item)
    {
        using var retryComCallsfilter = OleMessageFilter.MessageFilterRegister();

        SubscribeToStudioEvents();
        Project project = null;
        try
        {
            _ = item ?? throw new NullReferenceException("Item was null");
            var module = DownloadModule(item);
            var csProj = module.csProj
                         ?? throw new DirectoryNotFoundException("csproj file not found");

            if (EnvDte.Solution.IsOpen.Not())
            {
                EnvDte.Solution.Create(TempFiles.TempPath.ToUpper(), item.Host.Name);
            }

            try
            {
                project = EnvDte.Solution.AddFromFile(csProj.FullName);
            }
            catch
            {
                Logger?.Info("Can't add this project to solution");
                return false;
            }

            if (project.Object is not VSProject vsProject)
            {
                Logger?.Error($"{csProj.FullName} isn't a VSProject");
                return false;
            }

            var strategy = GetStrategy(project);
            strategy.InitProject(project, item, OdaFolder, VsixEx.VsixPath.FullName);

            UpdateAssemblyReferences(vsProject, AddinSettings.OdaLibraries);
            UpdateAssemblyReferences(vsProject, AddinSettings.UpdateReferenceLibraries);

            project.Save();
            var projectId = GetProjectGuid(project);

            oda.OdaOverride.INI.DebugINI.Write("DEBUG", item.FullId, true);
            if (!oda.OdaOverride.INI.DebugINI.Save())
            {
                throw new Exception("Can't save debug INI");
            }

            var moduleBuildInfo = new BuildInfo(project.UniqueName, module.remoteDir, module.localDir);
            LoadedModules.TryAdd(projectId, moduleBuildInfo);
        }
        catch (COMException ex)
        {
            if (ex.Message.Contains("E_FAIL"))
            {
                Logger?.Error("Visual studio returns error");
                return false;
            }

            Logger?.Error("Project was not initialized, Visual studio too busy");
            return false;
        }
        catch (Exception ex)
        {
            if (project != null)
            {
                EnvDte.Solution.Remove(project);
            }

            Logger?.Error($"Error while load project from item {item.Name}: {ex.Message}");
            return false;
        }

        return true;
    }

    public static (FileInfo csProj, Dir remoteDir, DirectoryInfo localDir) DownloadModule(StructureItem item)
    {
        var localDir = item.Dir.ServerToFolder();
        var moduleDir = localDir.GetDirectories("modules").FirstOrDefault();
        var csproj = moduleDir?.GetFiles("*.csproj")?.OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
        return (csproj, item.Dir, localDir);
    }

    private bool UpdateAssemblyReferences(VSProject vsProj, IEnumerable<string> references)
    {
        try
        {
            return UpdateAssemblyReferencesCore(vsProj, references);
        }
        catch (Exception e)
        {
            Logger?.Error($"Error while update assembly references : {e.Message}");
            return false;
        }
    }

    private string? UpdateAssemblyReference(Reference reference, HashSet<string> references)
    {
        var fullName = GetFullName(reference);
        var assemblyName = new AssemblyName(fullName);
        var isInUpdateList = references.Contains($"{assemblyName.Name}.dll");
        if (!isInUpdateList)
        {
            return null;
        }

        var newPath = Path.Combine(OdaFolder.FullName, $"{assemblyName.Name}.dll");
        var isExistsInOdantFolder = File.Exists(newPath);
        if (!isExistsInOdantFolder)
        {
            return null;
        }

        var isSameFileVersion = GetFileVersion(newPath) == GetFileVersion(reference.Path);
        var exists = File.Exists(reference.Path);
        if (exists && isSameFileVersion && !AddinSettings.ForceUpdateReferences)
        {
            return null;
        }

        reference.Remove();
        return $"{assemblyName.Name}.dll";
    }

    private bool UpdateAssemblyReferencesCore(VSProject vsProj, IEnumerable<string> references)
    {
        List<string> deletedDlls = [];
        var refs = references.ToHashSet();

        foreach (Reference reference in vsProj.References)
        {
            try
            {
                var updatedReference = UpdateAssemblyReference(reference, refs);
                if (updatedReference != null)
                {
                    deletedDlls.Add(updatedReference);
                }
            }
            catch (Exception e)
            {
                Logger?.Error($"Error while update assembly references : {e.Message}");
            }
        }

        foreach (var dll in refs)
        {
            if (deletedDlls.Contains(dll).Not())
            {
                continue;
            }

            var reference = (Reference3)vsProj.References.Add(Path.Combine(OdaFolder.FullName, dll));
            reference.CopyLocal = false;
            reference.SpecificVersion = false;

            Logger?.Info($"{dll} updated");
        }

        return true;
    }

    public static string GetFileVersion(string? path)
    {
        if (path == null) return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return versionInfo.FileVersion;
    }

    public static string GetFullName(Reference reference)
    {
        return
            $@"{reference.Name}, Version={reference.MajorVersion}.{reference.MinorVersion}.{reference.BuildNumber}.{reference.RevisionNumber}, Culture={reference.Culture.Or("neutral")}, PublicKeyToken={reference.PublicKeyToken.Or("null")}";
    }

    #endregion
}