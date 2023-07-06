using EnvDTE;

using EnvDTE80;

using MoreLinq;

using oda;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using VSLangProj;

using VSLangProj80;

using File = System.IO.File;
using Process = System.Diagnostics.Process;
using Task = System.Threading.Tasks.Task;

namespace OdantDev.Model;
public partial class VisualStudioIntegration
{
    #region Private Variables
    private BuildEvents BuildEvents { get; set; }
    private SolutionEvents SolutionEvents { get; set; }
    private DTE2 EnvDTE { get; }
    private ConcurrentDictionary<string, BuildInfo> LoadedModules { get; } = new();
    private AddinSettings AddinSettings { get; }
    private DirectoryInfo OdaFolder => new DirectoryInfo(AddinSettings.SelectedOdaFolder.Path);
    private ILogger Logger { get; }
    #endregion
    public VisualStudioIntegration(AddinSettings addinSettings, DTE2 DTE, ILogger logger = null)
    {
        this.Logger = logger;
        this.AddinSettings = addinSettings;

        EnvDTE = DTE
            ?? OdantDevApp.VSCommon.ExternalEnvDTE.Instance
            ?? throw new NullReferenceException("Can't get EnvDTE2 from visual studio");

        if (EnvDTE.Solution.IsOpen)
        {
            EnvDTE.Solution.Close();
        }
    }

    #region Visual studio events
    private void SubscribeToStudioEvents()
    {
        SolutionEvents = (EnvDTE.Events as Events2).SolutionEvents;
        SolutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;
        SolutionEvents.AfterClosing += SolutionEvents_AfterClosing;

        BuildEvents = (EnvDTE.Events as Events2).BuildEvents;
        BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
        BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
        BuildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;
    }

    private void UnsubscribeToStudioEvents()
    {
        SolutionEvents.ProjectRemoved -= SolutionEvents_ProjectRemoved;
        SolutionEvents.AfterClosing -= SolutionEvents_AfterClosing;
        SolutionEvents = null;
        BuildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;
        BuildEvents.OnBuildDone -= BuildEvents_OnBuildDone;
        BuildEvents.OnBuildProjConfigDone -= BuildEvents_OnBuildProjConfigDone;
        BuildEvents = null;
    }

    // Если закрыть Solution и потом открыть модуль он попадает в коллекцию, после этого открывается Solution и событие очищает коллекцию модулей
    // добавил эвент SolutionEvents_AfterClosing, думаю в таком случае будет правильнее там очищать
    private void SolutionEvents_AfterClosing()
    {
        LoadedModules.Clear();
        UnsubscribeToStudioEvents();
        oda.OdaOverride.INI.DebugINI.Clear();
        oda.OdaOverride.INI.DebugINI.Save();
    }

    private void SolutionEvents_ProjectRemoved(Project Project)
    {
        var guid = GetProjectGuid(Project);
        if (LoadedModules.TryGetValue(guid, out var loadedModules))
        {
            loadedModules.Dispose();
            LoadedModules.TryRemove(guid, out _);
        }
    }

    private async void BuildEvents_OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
    {
        Project project = EnvDTE.Solution.Item(Project);
        if (LoadedModules.TryGetValue(GetProjectGuid(project), out var buildInfo))
        {
            buildInfo.isBuildSuccess = Success;
            if (Success.Not())
            {
                EnvDTE.ExecuteCommand("Build.Cancel");
            }
        }
    }


    public async void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
    {
        if ((Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll)) { return; }
        if (LoadedModules.Values.ToList().TrueForAll(x => x.isBuildSuccess).Not())
        {
            LoadedModules.Values.ForEach(x => x.isBuildSuccess = true);
            return;
        }
        foreach (Project project in (EnvDTE.ActiveSolutionProjects as object[]).Cast<Project>())
        {
            await CopyToOdaBinAsync(project);
        }
    }

    public async void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
    {
        if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
        VSErrors.Clear();
        foreach (Project project in (EnvDTE.ActiveSolutionProjects as object[]).Cast<Project>())
        {
            if ((await IncreaseVersionAsync(project)).Not())
            {
                EnvDTE.ExecuteCommand("Build.Cancel");
            }
        }
    }

    #endregion

    #region Common Methods for Project item
    public async Task<bool> CopyToOdaBinAsync(Project project)
    {
        try
        {
            var currentDirectory = new FileInfo(project.FullName).Directory;
            var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == "AssemblyInfo.cs")
                ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");
            var version = new Version(
                assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>().FirstOrDefault(x => x.Name == "AssemblyVersion")?.Value?.Replace("\"", string.Empty)
                ?? throw new NullReferenceException(@$"Missing AssemblyVersion in {currentDirectory}\AssemblyInfo.cs"));
            var versionPath = @$"{version.Major}.{version.Minor}\{version.Build}\{version.Revision}";

            var outDirParent = new FileInfo(project.FullName).Directory.Parent;
            outDirParent.CreateSubdirectory("bin").TryDeleteDirectory();
            var outputDir = outDirParent.CreateSubdirectory("bin");
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

            var isCopyBinToServerSuccess = remoteVersionDir.SaveFile(moduleDir.BinInfo.FullName);

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
                    if (x is FileInfo fileInfo && x.Extension == ".dll")
                    {
                        var isValidVersion = Version.TryParse(FileVersionInfo.GetVersionInfo(x.FullName).FileVersion, out var FileVersion);
                        x.CopyToDir(isValidVersion ? clientRefDir.CreateSubdirectory(FileVersion.ToString()) : clientRefDir);
                        remoteRefDir.SaveFile(x.FullName);
                    }
                });
            }
            CopyModuleDirToServer(buildInfo);
        }
        catch (Exception ex)
        {
            await project.ShowError($"Error in Method: {MethodBase.GetCurrentMethod().Name}. Message: {ex.Message}");
        }
        return true;
    }

    private bool CopyModuleDirToServer(BuildInfo buildInfo)
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

    public async Task<bool> IncreaseVersionAsync(Project project)
    {
        try
        {
            //await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == "AssemblyInfo.cs")
                ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");
            var version = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>().FirstOrDefault(x => x.Name == "AssemblyVersion");
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
            if (assemblyInfo.IsOpen.Not())
            {
                assemblyInfo.Open();
            }
            assemblyInfo.Save();
            return true;
        }
        catch (Exception ex)
        {
            await project.ShowError($"Error in Method: {MethodBase.GetCurrentMethod().Name}. Message: {ex.Message}");
            return false;
        }
    }

    private string GetProjectGuid(Project project)
    {
        return project.UniqueName;
    }

    private void SetAttributeToProjectItem(IDictionary<string, CodeAttribute2> codeAttributes, ProjectItem projectItem, string name, string value)
    {
        codeAttributes.TryGetValue(name, out var attribute);
        if (attribute == null)
        {
            projectItem.FileCodeModel.AddAttribute(name, $"\"{value}\"");
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
        if (BuildEvents is null)
        {
            SubscribeToStudioEvents();
        }
        Project project = null;
        VSErrors.Clear();
        try
        {
            _ = item ?? throw new NullReferenceException("Item was null");
            var module = DownloadModule(item);
            var csProj = module.csProj ?? throw new DirectoryNotFoundException("Module csproj file not found");

            if (EnvDTE.Solution.IsOpen.Not())
            {
                EnvDTE.Solution.Create(TempFiles.TempPath.ToUpper(), item.Host.Name);
            }

            project = EnvDTE.Solution.AddFromFile(csProj.FullName);
            await InitProject(project, item);
            UpdateAssemblyReferences(project.Object as VSProject, AddinSettings.OdaLibraries);
            UpdateAssemblyReferences(project.Object as VSProject, AddinSettings.UpdateReferenceLibraries);
            oda.OdaOverride.INI.DebugINI.Write("DEBUG", item.FullId, true);
            if (!await oda.OdaOverride.INI.DebugINI.SaveAsync())
            {
                throw new Exception("Can't save debug INI");
            }
            await IncreaseVersionAsync(project);
            project.Save();
            LoadedModules.TryAdd(GetProjectGuid(project), new BuildInfo(project.UniqueName, module.remoteDir, module.localDir));
        }
        catch (Exception ex)
        {
            if (project != null)
            {
                EnvDTE.Solution.Remove(project);
            }
            await EnvDTE.ShowError($"Error while load project from item {item.Name}: {ex.Message}");
            return false;
        }
        return true;
    }

    public (FileInfo csProj, Dir remoteDir, DirectoryInfo localDir) DownloadModule(StructureItem item)
    {
        var localDir = item.Dir.ServerToFolder();
        var moduleDir = localDir.GetDirectories("modules").FirstOrDefault();
        return (moduleDir
            ?.GetFiles("*.csproj")
            ?.OrderByDescending(x => x.LastWriteTime)
            .FirstOrDefault(), item.Dir, localDir);
    }

    private async Task InitProject(Project project, StructureItem sourceItem)
    {
        if (project == null) { throw new NullReferenceException(nameof(project)); }
        project.Name = $"{sourceItem.Name}-{sourceItem.Id}";

        project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartAction").Value = prjStartAction.prjStartActionProgram;
        var startProgram = VsixExtension.Platform == Bitness.x64 ? "ODA.exe" : "oda.wrapper32.exe";
        project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartProgram").Value = Path.Combine(OdaFolder.FullName, startProgram);
        project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartArguments").Value = "debug";
        project.Properties.Item("AssemblyName").Value = project.Name;
        project.Properties.Item("ReferencePath").Value = OdaFolder.FullName;
        var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == "AssemblyInfo.cs");
        var assemblyFile = (@$"{new FileInfo(project.FullName).Directory}\AssemblyInfo.cs");
        if (assemblyInfo == null || File.Exists(assemblyFile).Not())
        {
            if (File.Exists(assemblyFile))
            {
                File.Delete(assemblyFile);
            }
            assemblyInfo = project.ProjectItems.AddFromFileCopy(Path.Combine(VsixExtension.VSIXPath.FullName, @"Templates\AssemblyInfo.cs"));
        }
        var attributes = assemblyInfo.FileCodeModel?.CodeElements?.OfType<CodeAttribute2>()?.ToDictionary(x => x.Name);
        if (attributes != null)
        {
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyTitle", $"{sourceItem.Name}-{sourceItem.Id}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyDescription", sourceItem.Hint ?? string.Empty);
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyDefaultAlias", $"{sourceItem.Name}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyCompany", $"Infostandart");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyTrademark", $"www.infostandart.com");
        }
        if (assemblyInfo.IsOpen.Not()) { assemblyInfo.Open(); }
        assemblyInfo.Save();
    }

    private bool UpdateAssemblyReferences(VSProject VSProj, IEnumerable<string> references)
    {
        try
        {
            List<string> deletedDlls = new();
            foreach (Reference reference in VSProj.References)
            {
                var assemblyName = new AssemblyName(GetFullName(reference));
                var newPath = Path.Combine(OdaFolder.FullName, $"{assemblyName.Name}.dll");
                var notExist = File.Exists(reference.Path).Not();
                var isInUpdateList = references.Contains($"{assemblyName.Name}.dll");

                if (isInUpdateList && 
                    (notExist || GetFileVersion(newPath) != GetFileVersion(reference.Path) || AddinSettings.ForceUpdateReferences))
                {
                    reference.Remove();
                    deletedDlls.Add($"{assemblyName.Name}.dll");
                }
            }
            foreach (var dll in references)
            {
                if (deletedDlls.Contains(dll).Not()) { continue; }
                var reference = (Reference3)VSProj.References.Add(Path.Combine(OdaFolder.FullName, dll));
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

    public static string GetFileVersion(string path)
    {
        if (path == null) return string.Empty;

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return versionInfo.FileVersion;
    }

    public static string GetFullName(Reference reference)
    {
        return string.Format(@"{0}, Version={1}.{2}.{3}.{4}, Culture={5}, PublicKeyToken={6}",
            reference.Name,
            reference.MajorVersion, reference.MinorVersion, reference.BuildNumber, reference.RevisionNumber,
            reference.Culture.Or("neutral"),
            reference.PublicKeyToken.Or("null"));
    }

    #endregion
}
