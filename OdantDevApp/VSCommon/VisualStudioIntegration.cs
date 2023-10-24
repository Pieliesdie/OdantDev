using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

using EnvDTE;

using EnvDTE80;

using oda;

using OdantDevApp.VSCommon;

using VSLangProj;

using VSLangProj80;

using File = System.IO.File;
using Task = System.Threading.Tasks.Task;

namespace OdantDev.Model;

public sealed partial class VisualStudioIntegration
{
    #region Private Variables
    private bool IsLastBuildSuccess { get; set; } = true;
    private bool FireEvents { get; set; } = false;
    private BuildEvents? BuildEvents { get; set; }
    private SolutionEvents? SolutionEvents { get; set; }
    private DTE2 EnvDte { get; }
    private ConcurrentDictionary<string, BuildInfo> LoadedModules { get; } = new();
    private OdantDevApp.Model.ViewModels.AddinSettings AddinSettings { get; }
    private DirectoryInfo OdaFolder => new(AddinSettings.SelectedOdaFolder.Path);
    private ILogger? Logger { get; }
    private Project[] ActiveSolutionProjects => ((EnvDte.ActiveSolutionProjects as object[]) ?? Array.Empty<Project>()).Cast<Project>().ToArray();
    #endregion

    public static bool IsVisualStudioDark(DTE2? dte)
    {
        try
        {
            if (dte == null) return false;
            var uintClr = dte.GetThemeColor(vsThemeColors.vsThemeColorToolWindowBackground);
            byte[] bytes = BitConverter.GetBytes(uintClr);
            var defaultBackground = Color.FromArgb(bytes[3], bytes[0], bytes[1], bytes[2]);
            var isDarkTheme = (384 - defaultBackground.R - defaultBackground.G - defaultBackground.B) > 0;
            return isDarkTheme;
        }
        catch
        {
            return false;
        }
    }

    public VisualStudioIntegration(OdantDevApp.Model.ViewModels.AddinSettings addinSettings, DTE2 dte, ILogger logger = null)
    {
        this.Logger = logger;
        this.AddinSettings = addinSettings;

        EnvDte = dte
            ?? throw new NullReferenceException("Can't get EnvDTE2 from visual studio");

        try
        {
            using var retryComCallsfilter = MessageFilter.MessageFilterRegister();
            if (EnvDte.Solution.IsOpen)
            {
                EnvDte.Solution.Close();
            }
        }
        catch { }
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
        if (LoadedModules.TryGetValue(guid, out _))
        {
            LoadedModules.TryRemove(guid, out _);
        }
    }

    private void BuildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
    {
        if (!FireEvents) return;
        IsLastBuildSuccess = success;
    }

    private void BuildEvents_OnBuildDone(vsBuildScope scope, vsBuildAction action)
    {
        if (!FireEvents) return;
        if ((action != vsBuildAction.vsBuildActionBuild && action != vsBuildAction.vsBuildActionRebuildAll)) { return; }
        if (!IsLastBuildSuccess) { return; }

        foreach (var project in ActiveSolutionProjects)
        {
            if (!LoadedModules.TryGetValue(GetProjectGuid(project), out _)) { continue; }

            CopyToOdaBin(project);
        }
    }

    private void BuildEvents_OnBuildBegin(vsBuildScope scope, vsBuildAction action)
    {
        if (!FireEvents) return;
        if (action != vsBuildAction.vsBuildActionBuild && action != vsBuildAction.vsBuildActionRebuildAll) { return; }

        foreach (var project in ActiveSolutionProjects)
        {
            if (!LoadedModules.TryGetValue(GetProjectGuid(project), out _)) { continue; }

            if (IncreaseVersion(project).Not())
            {
                EnvDte.ExecuteCommand("Build.Cancel");
                return;
            }
        }
    }
    #endregion

    #region Common Methods for Project item
    private static ProjectItem? FindProjectItem(Project project, string name)
    {
        return project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == name);
    }
    private static CodeAttribute2? FindCodeAttribute(ProjectItem projectItem, string name)
    {
        return projectItem.FileCodeModel.CodeElements.OfType<CodeAttribute2>().FirstOrDefault(x => x.Name == name);
    }
    private static void SaveProjectItem(ProjectItem projectItem)
    {
        if (projectItem.IsOpen.Not())
        {
            projectItem.Open();
        }
        projectItem.Save();
    }
    private static void SetProperties(Properties properties, IReadOnlyDictionary<string, object> values)
    {
        var valueCount = values.Count;
        var iterationCount = 0;
        foreach (Property property in properties)
        {
            if (values.TryGetValue(property.Name, out var value))
            {
                property.Value = value;
            }
            iterationCount++;

            if (iterationCount == valueCount)
                break;
        }
    }
    private bool CopyToOdaBin(Project project)
    {
        try
        {
            var currentDirectory = new FileInfo(project.FullName).Directory;
            var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs")
                ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");

            var version = new Version(VisualStudioIntegration.FindCodeAttribute(assemblyInfo, "AssemblyVersion")?.Value?.Replace("\"", string.Empty)
                ?? throw new NullReferenceException(@$"Missing AssemblyVersion in {currentDirectory}\AssemblyInfo.cs"));

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

            if ((remoteVersionDir.SaveFile(moduleDir.BinInfo.FullName) && remoteVersionDir.SaveFile(moduleDir.PdbInfo.FullName)).Not())
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

                    var isValidVersion = Version.TryParse(FileVersionInfo.GetVersionInfo(x.FullName).FileVersion, out var fileVersion);
                    x.CopyToDir(isValidVersion ? clientRefDir.CreateSubdirectory(fileVersion.ToString()) : clientRefDir);
                    remoteRefDir.SaveFile(x.FullName);
                });
            }
            CopyModuleDirToServer(buildInfo);
        }
        catch (Exception ex)
        {
            Logger?.Error($"Error in Method: {MethodBase.GetCurrentMethod()?.Name}. Message: {ex.Message}");
            return false;
        }
        return true;
    }

    private static bool CopyModuleDirToServer(BuildInfo buildInfo)
    {
        try
        {
            var isCopied = buildInfo.RemoteDir.Class.Dir.OpenOrCreateFolder("modules").FolderToServer(buildInfo.LocalDir.CreateSubdirectory("modules"));
            var isSaved = buildInfo.RemoteDir.Class.Save();
            buildInfo.RemoteDir.Class.Dir.Reset();
            return isCopied && isSaved;
        }
        catch
        {
            return false;
        }
    }

    private bool IncreaseVersion(Project project)
    {
        try
        {
            var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs")
                ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");

            var version = FindCodeAttribute(assemblyInfo, "AssemblyVersion");
            if (version == null)
            {
                assemblyInfo.FileCodeModel.AddAttribute("AssemblyVersion", $"\"{Utils.Version}\"");
            }
            else
            {
                var currentVersion = Version.Parse(version.Value.Replace("\"", string.Empty));
                var currentOdantVersion = Version.Parse(Utils.Version);
                version.Value = $"\"{Utils.MajorVersion}.{Utils.ShortVersion}.{Math.Max(currentOdantVersion.Revision, currentVersion.Revision + 1)}\"";
            }
            SaveProjectItem(assemblyInfo);
            return true;
        }
        catch (Exception ex)
        {
            Logger?.Error($"Error in Method: {MethodBase.GetCurrentMethod()?.Name}. Message: {ex.Message}");
            return false;
        }
    }

    private static string GetProjectGuid(Project project)
    {
        return project?.UniqueName;
    }

    private static void SetAttributeToProjectItem(ProjectItem projectItem, string name, string value)
    {
        var attribute = projectItem.FileCodeModel?.CodeElements?.OfType<CodeAttribute2>()?.FirstOrDefault(x => x.Name == name);
        if (attribute == null)
        {
            projectItem.FileCodeModel?.AddAttribute(name, $"\"{value}\"");
        }
        else
        {
            attribute.Value = $"\"{value}\"";
        }
    }
    #endregion

    #region Download and init  logic for Module

    public async Task<bool> OpenModuleAsync(StructureItem item)
    {
        bool result = false;
        System.Threading.Thread staThread = new System.Threading.Thread(() => result = OpenModule(item));
        staThread.SetApartmentState(ApartmentState.STA); //COM needs STA
        staThread.Start();
        await Task.Run(staThread.Join);
        return result;
    }

    private bool OpenModule(StructureItem item)
    {
        using var retryComCallsfilter = MessageFilter.MessageFilterRegister();

        SubscribeToStudioEvents();
        Project project = null;
        try
        {
            _ = item ?? throw new NullReferenceException("Item was null");
            var module = DownloadModule(item);
            var csProj = module.csProj
                ?? throw new DirectoryNotFoundException("Module csproj file not found");

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
            InitProject(project, item);
            UpdateAssemblyReferences(vsProject, OdantDevApp.Model.ViewModels.AddinSettings.OdaLibraries);
            UpdateAssemblyReferences(vsProject, AddinSettings.UpdateReferenceLibraries);

            if (!IncreaseVersion(project))
            {
                return false;
            }

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
        return (moduleDir
            ?.GetFiles("*.csproj")
            ?.OrderByDescending(x => x.LastWriteTime)
            .FirstOrDefault(), item.Dir, localDir);
    }

    private void InitProject(Project project, Item sourceItem)
    {
        if (project == null) { throw new NullReferenceException(nameof(project)); }

        var startProgram = VsixExtension.Platform == Bitness.x64 ? "ODA.exe" : "oda.wrapper32.exe";
        var assemblyFile = (@$"{new FileInfo(project.FullName).Directory}\AssemblyInfo.cs");

        project.Name = $"{sourceItem.Name}-{sourceItem.Id}";

        Dictionary<string, object> configurationAttributes = new()
        {
            { "StartAction", prjStartAction.prjStartActionProgram },
            { "StartProgram", Path.Combine(OdaFolder.FullName, startProgram) },
            { "StartArguments", "debug" }
        };
        VisualStudioIntegration.SetProperties(project.ConfigurationManager.ActiveConfiguration.Properties, configurationAttributes);

        Dictionary<string, object> projectAttributes = new()
        {
            { "AssemblyName", project.Name },
            { "ReferencePath", OdaFolder.FullName }
        };
        VisualStudioIntegration.SetProperties(project.Properties, projectAttributes);

        var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs");
        if (assemblyInfo == null || File.Exists(assemblyFile).Not())
        {
            if (File.Exists(assemblyFile))
            {
                File.Delete(assemblyFile);
            }
            assemblyInfo = project.ProjectItems.AddFromFileCopy(Path.Combine(VsixExtension.VSIXPath.FullName, @"Templates\AssemblyInfo.cs"));
        }

        SetAttributeToProjectItem(assemblyInfo, "AssemblyTitle", $"{sourceItem.Name}-{sourceItem.Id}");
        SetAttributeToProjectItem(assemblyInfo, "AssemblyDescription", sourceItem.Hint ?? string.Empty);
        SetAttributeToProjectItem(assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
        SetAttributeToProjectItem(assemblyInfo, "AssemblyDefaultAlias", $"{sourceItem.Name}");
        SetAttributeToProjectItem(assemblyInfo, "AssemblyCompany", $"Infostandart");
        SetAttributeToProjectItem(assemblyInfo, "AssemblyTrademark", $"www.infostandart.com");

        SaveProjectItem(assemblyInfo);
    }

    private bool UpdateAssemblyReferences(VSProject vsProj, IEnumerable<string> references)
    {
        try
        {
            List<string> deletedDlls = new();
            var refs = references.ToList();
            foreach (Reference reference in vsProj.References)
            {
                var assemblyName = new AssemblyName(GetFullName(reference));
                var newPath = Path.Combine(OdaFolder.FullName, $"{assemblyName.Name}.dll");
                var notExist = File.Exists(reference.Path).Not();
                var isInUpdateList = refs.Contains($"{assemblyName.Name}.dll");

                if (isInUpdateList &&
                    (notExist || GetFileVersion(newPath) != GetFileVersion(reference.Path) || AddinSettings.ForceUpdateReferences))
                {
                    reference.Remove();
                    deletedDlls.Add($"{assemblyName.Name}.dll");
                }
            }
            foreach (var dll in refs)
            {
                if (deletedDlls.Contains(dll).Not()) { continue; }
                var reference = (Reference3)vsProj.References.Add(Path.Combine(OdaFolder.FullName, dll));
                reference.CopyLocal = false;
                reference.SpecificVersion = false;
                Logger?.Info($"{dll} updated");
            }
            return true;
        }
        catch
        {
            return false;
        }
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
