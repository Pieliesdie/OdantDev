using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using oda;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VSLangProj;
using File = System.IO.File;

namespace OdantDev.Model
{
    public partial class VisualStudioIntegration
    {
        #region Global Variables
        private BuildEvents BuildEvents { get; }
        private SolutionEvents SolutionEvents { get; }
        private ProjectsEvents ProjectsEvents { get; }
        private DTE2 envDTE { get; }
        private IServiceProvider serviceProvider { get; }
        private Dictionary<Guid, BuildInfo> LoadedModules { get; } = new Dictionary<Guid, BuildInfo>();
        private DirectoryInfo AddinFolder { get; }
        private DirectoryInfo OdaFolder { get; }
        #endregion
        public VisualStudioIntegration(DirectoryInfo odaFolder, DTE2 DTE)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OdaFolder = odaFolder;
            AddinFolder = odaFolder.CreateSubdirectory("AddIn");
            envDTE = DTE
                ?? throw new NullReferenceException("Can't get EnvDTE from visual studio");
            serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)envDTE)
                ?? throw new NullReferenceException("Can't get ServiceProvider from vusial studio's EnvDTE");
            if (envDTE.Solution.IsOpen)
            {
                envDTE.Solution.Close();
            }
            SolutionEvents = (DTE.Events as Events2).SolutionEvents;
            SolutionEvents.ProjectRemoved += SolutionEvents_ProjectRemoved;
            SolutionEvents.Opened += SolutionEvents_Opened;

            ProjectsEvents = (DTE.Events as Events2).ProjectsEvents;

            BuildEvents = (DTE.Events as Events2).BuildEvents;
            BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
            BuildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;
            BuildEvents.OnBuildProjConfigBegin += BuildEvents_OnBuildProjConfigBegin;
        }
        #region Visual studio events
        private void SolutionEvents_Opened()
        {
            LoadedModules.Clear();
        }
        private void SolutionEvents_ProjectRemoved(Project Project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var guid = GetProjectGuid(Project);
            LoadedModules[GetProjectGuid(Project)].Dispose();
            LoadedModules.Remove(GetProjectGuid(Project));
        }
        private void BuildEvents_OnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
        {

        }
        private void BuildEvents_OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Project project = envDTE.Solution.Item(Project);
            LoadedModules[GetProjectGuid(project)].isBuildSuccess = Success;
            if (Success.Not())
            {
                envDTE.ExecuteCommand("Build.Cancel");
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            if ((Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll)) { return; }
            if (LoadedModules.Values.ToList().TrueForAll(x => x.isBuildSuccess).Not()) { return; }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in envDTE.ActiveSolutionProjects as object[])
            {
                CopyToOdaBin(project);
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VSErrors.Clear();
            foreach (Project project in envDTE.ActiveSolutionProjects as object[])
            {
                if (IncreaseVersion(project).Not())
                {
                    envDTE.ExecuteCommand("Build.Cancel");
                }
            }
        }
        #endregion
        #region Common Methods for Project item
        public bool CopyToOdaBin(Project project)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                var currentDirectory = new FileInfo(project.FullName).Directory;
                var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == "AssemblyInfo.cs")
                    ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");
                var version = new Version(
                    assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>().FirstOrDefault(x => x.Name == "AssemblyVersion")?.Value?.Replace("\"", string.Empty)
                    ?? throw new NullReferenceException(@$"Missing AssemblyVersion in {currentDirectory}\AssemblyInfo.cs"));
                var versionPath = @$"{version.Major}.{version.Minor}\{version.Build}\{version.Revision}";

                var outputDir = new FileInfo(project.FullName).Directory.Parent.CreateSubdirectory("bin").Clear();
                var outputBinDir = outputDir.CreateSubdirectory(versionPath);
                var moduleDir = new ModuleDir(project);
                var clientBinDir = OdaFolder.CreateSubdirectory(Path.Combine("bin", versionPath, moduleDir.Name));

                moduleDir.BinInfo.CopyToDir(clientBinDir);
                moduleDir.BinInfo.CopyToDir(outputBinDir);
                moduleDir.PdbInfo.CopyToDir(outputBinDir);

                var buildInfo = LoadedModules[GetProjectGuid(project)];
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
                project.ShowError($"Error in Method: {System.Reflection.MethodBase.GetCurrentMethod().Name}. Message: {ex.Message}");
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
        public bool IncreaseVersion(Project project)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
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
                project.ShowError($"Error in Method: {System.Reflection.MethodBase.GetCurrentMethod().Name}. Message: {ex.Message}");
                return false;
            }
        }
        private Guid GetProjectGuid(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            IVsHierarchy hierarchy;

            solution.GetProjectOfUniqueName(project.FullName, out hierarchy);
            if (hierarchy == null) { return Guid.Empty; }
            hierarchy.GetGuidProperty(
                        VSConstants.VSITEMID_ROOT,
                        (int)__VSHPROPID.VSHPROPID_ProjectIDGuid,
                        out var projectGuid);
            return projectGuid;
        }
        private void SetAttributeToProjectItem(IDictionary<string, CodeAttribute2> codeAttributes, ProjectItem projectItem, string name, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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
            Project project = null;
            try
            {
                _ = item ?? throw new NullReferenceException("Item was null");
                var module = DownloadModule(item);
                var csProj = module.csProj ?? throw new DirectoryNotFoundException("Module csproj file not found");
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if (envDTE.Solution.IsOpen.Not())
                {
                    envDTE.Solution.Create(AddinFolder.FullName, item.Host.Name);
                }

                project = envDTE.Solution.AddFromFile(csProj.FullName);
                InitProject(project, item);
                Common.DebugINI.Write("DEBUG", item.FullId, true);
                IncreaseVersion(project);
                LoadedModules.Add(GetProjectGuid(project), new BuildInfo(project.UniqueName, module.remoteDir, module.localDir));
            }
            catch (Exception ex)
            {
                project?.Delete();
                envDTE.ShowError($"Error while load project from item {item.Name}: {ex.Message}");
                return false;
            }
            return true;
        }
        public Task<(FileInfo csProj, Dir moduleDir, DirectoryInfo localDir)> DownloadModuleAsync(StructureItem item) => Task.Run(() => DownloadModule(item));
        public (FileInfo csProj, Dir remoteDir, DirectoryInfo localDir) DownloadModule(StructureItem item)
        {
            var localDir = item.Dir.ServerToFolder();
            var moduleDir = localDir.GetDirectories("modules").FirstOrDefault();
            return (moduleDir
                ?.GetFiles("*.csproj")
                ?.OrderByDescending(x => x.LastWriteTime)
                .FirstOrDefault(), item.Dir, localDir);
        }
        private void InitProject(Project project, StructureItem sourceItem)
        {
            if (project == null) { throw new NullReferenceException(nameof(project)); }
            ThreadHelper.ThrowIfNotOnUIThread();
            project.Name = $"{sourceItem.Name}-{sourceItem.Id}";
            project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartAction").Value = prjStartAction.prjStartActionProgram;
            project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartProgram").Value = Path.Combine(OdaFolder.FullName, "oda.wrapper32.exe");
            project.ConfigurationManager.ActiveConfiguration.Properties.Item("StartArguments").Value = "debug";
            var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().Where(x => x.Name == "AssemblyInfo.cs").FirstOrDefault();
            var assemblyFile = (@$"{new FileInfo(project.FullName).Directory}\AssemblyInfo.cs");
            if (assemblyInfo == null || File.Exists(assemblyFile).Not())
            {
                if (File.Exists(assemblyFile))
                {
                    File.Delete(assemblyFile);
                }
                assemblyInfo = project.ProjectItems.AddFromFileCopy(@"Templates\AssemblyInfo.cs");
            }
            var attributes = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>().ToDictionary(x => x.Name);
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyTitle", $"{sourceItem.Name}-{sourceItem.Id}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyDescription", sourceItem.Hint ?? string.Empty);
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyDefaultAlias", $"{sourceItem.Name}");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyCompany", $"Infostandart");
            SetAttributeToProjectItem(attributes, assemblyInfo, "AssemblyTrademark", $"www.infostandart.com");
            if (assemblyInfo.IsOpen.Not()) { assemblyInfo.Open(); }
            assemblyInfo.Save();
        }
        #endregion
    }
}
