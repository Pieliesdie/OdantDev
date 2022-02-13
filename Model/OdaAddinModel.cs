using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using oda;
using System;
using System.Collections.Generic;
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

        private IServiceProvider serviceProvider { get; }
        public DirectoryInfo AddinFolder { get; }
        public OdaAddinModel(DirectoryInfo odaFolder, DTE2 DTE)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            AddinFolder = odaFolder.CreateSubdirectory("AddIn");
            envDTE = DTE
                ?? throw new NullReferenceException("Can't get EnvDTE from visual studio");
            serviceProvider = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)DTE)
                ?? throw new NullReferenceException("Can't get ServiceProvider from visual studio"); ;
            if (envDTE.Solution.IsOpen)
            {
                envDTE.Solution.Close();
            }
            BuildEvents = DTE.Events.BuildEvents;
            BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
        }
        public bool IncreaseVersion(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().Where(x => x.Name == "AssemblyInfo.cs").FirstOrDefault();
            if (assemblyInfo == null) { throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}"); }
            var version = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>().Where(x => x.Name == "AssemblyVersion").FirstOrDefault();
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
            return true;
        }

        public bool CopyToOdaBin(Project project)
        {

            return true;
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
        public Task<FileInfo> DownloadModuleAsync(StructureItem item) => System.Threading.Tasks.Task.Run(() => DownloadModule(item));
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
                ValidateProject(project,item);
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void ValidateProject(Project project,StructureItem sourceItem)
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
                assemblyInfo =  project.ProjectItems.AddFromFileCopy(@"Templates\AssemblyInfo.cs");
            }
            var attributes = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>();
            setAttr(attributes, assemblyInfo, "AssemblyTitle", project.Name);
            setAttr(attributes, assemblyInfo, "AssemblyDescription", sourceItem.Description ?? string.Empty);
            setAttr(attributes, assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");

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
