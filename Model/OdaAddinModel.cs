using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using oda;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using File = System.IO.File;

namespace OdantDev.Model
{
    public class OdaAddinModel
    {
        private BuildEvents BuildEvents { get; }
        private DTE2 envDTE { get; }
        public DirectoryInfo AddinFolder { get; }
        public DirectoryInfo OdaFolder { get; }
        public OdaAddinModel(DirectoryInfo odaFolder, DTE2 DTE)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OdaFolder = odaFolder;
            AddinFolder = odaFolder.CreateSubdirectory("AddIn");
            envDTE = DTE
                ?? throw new NullReferenceException("Can't get EnvDTE from visual studio");
            if (envDTE.Solution.IsOpen)
            {
                envDTE.Solution.Close();
            }
            BuildEvents = (DTE.Events as Events2).BuildEvents;
            BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in envDTE.Solution.Projects)
            {
                CopyToOdaBin(project);
            }

        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in envDTE.Solution.Projects)
            {
                IncreaseVersion(project);
            }
        }
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
                if (moduleDir.Refs.Any())
                {
                    var refDir = outputDir.CreateSubdirectory("ref");
                    var clientRefDir = OdaFolder.CreateSubdirectory(Path.Combine("bin", "ref"));
                    moduleDir.Refs.ToList().ForEach(x =>
                    {
                        x.CopyToDir(refDir);
                        if (x is FileInfo fileInfo && x.Extension == ".dll")
                        {
                            var isValidVersion = Version.TryParse( FileVersionInfo.GetVersionInfo(x.FullName).FileVersion, out var FileVersion);
                            x.CopyToDir(isValidVersion? clientRefDir.CreateSubdirectory(FileVersion.ToString()) : clientRefDir);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                project.ShowError($"Error in Method: {System.Reflection.MethodBase.GetCurrentMethod().Name}. Message: {ex.Message}");
            }
            return true;
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
                envDTE.ExecuteCommand("Build.Cancel");
                return false;
            }
        }
        public Task<FileInfo> DownloadModuleAsync(StructureItem item) => Task.Run(() => DownloadModule(item));
        public FileInfo DownloadModule(StructureItem item)
        {
            using var moduleDir = item.Dir.GetDir("modules");
            return moduleDir
                .ServerToFolder()
                .GetFiles("*.csproj")
                .OrderByDescending(x => x.LastWriteTime)
                .FirstOrDefault();
        }
        public async Task<bool> OpenModuleAsync(StructureItem item)
        {
            _ = item ?? throw new NullReferenceException("Item was null");
            var moduleDir = DownloadModule(item) ?? throw new DirectoryNotFoundException("Module csproj file not found");
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (envDTE.Solution.IsOpen.Not())
            {
                envDTE.Solution.Create(AddinFolder.FullName, item.Host.Name);
            }
            try
            {
                var project = envDTE.Solution.AddFromFile(moduleDir.FullName);
                ValidateProject(project, item);
            }
            catch
            {
                return false;
            }
            return true;
        }
        private void ValidateProject(Project project, StructureItem sourceItem)
        {
            if (project == null) { throw new NullReferenceException(nameof(project)); }
            ThreadHelper.ThrowIfNotOnUIThread();
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
            var attributes = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>();
            setAttr(attributes, assemblyInfo, "AssemblyTitle", $"{sourceItem.Name}-{sourceItem.Id}");
            setAttr(attributes, assemblyInfo, "AssemblyDescription", sourceItem.Hint ?? string.Empty);
            setAttr(attributes, assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
            setAttr(attributes, assemblyInfo, "AssemblyMetadata", $"ModuleName\",\"{sourceItem.Name}");
            if (assemblyInfo.IsOpen.Not()) { assemblyInfo.Open(); }
            assemblyInfo.Save();
        }
        private void setAttr(IEnumerable<CodeAttribute2> codeAttributes, ProjectItem projectItem, string name, string value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var attribute = codeAttributes.Where(x => x.Name == name).FirstOrDefault();
            if (attribute == null)
            {
                projectItem.FileCodeModel.AddAttribute(name, $"\"{value}\"");
            }
            else
            {
                attribute.Value = $"\"{value}\"";
            }
        }
    }
}
