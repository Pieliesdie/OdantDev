using EnvDTE;
using EnvDTE80;
using oda;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdantDev.Model
{
    public class OdaAddinModel
    {
        private BuildEvents BuildEvents { get; }
        public DTE2 EnvDTE { get; }
        public DirectoryInfo AddinFolder { get; }
        public OdaAddinModel(DirectoryInfo odaFolder, DTE2 DTE)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            AddinFolder = odaFolder.CreateSubdirectory("AddIn");
            EnvDTE = DTE;
            if (EnvDTE.Solution.IsOpen)
            {
                EnvDTE.Solution.Close();
            }
            /*BuildEvents = DTE.Events.BuildEvents;
            BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
            BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;*/
        }
        public bool IncreaseVersion(Project project)
        {
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            //throw new NotImplementedException();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
        public async void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            if (Action != vsBuildAction.vsBuildActionBuild && Action != vsBuildAction.vsBuildActionRebuildAll) { return; }
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Project project in EnvDTE.Solution.Projects)
            {
                IncreaseVersion(project);
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
            await Microsoft.VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (EnvDTE.Solution.IsOpen.Not())
            {
                EnvDTE.Solution.Create(AddinFolder.FullName, item.Host.Name);
            }
            EnvDTE.Solution.AddFromFile(moduleDir.FullName);
            return true;
        }

    }
}
