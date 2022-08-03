using EnvDTE;
using EnvDTE80;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OdantDev.Model
{
    internal class ModuleDir
    {
        public string Name { get; }
        public FileInfo BinInfo { get; }
        public FileInfo PdbInfo { get; }
        public IEnumerable<FileSystemInfo> Refs { get; }

        public ModuleDir(Project project)
        {
            Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
            var assemblyInfo = project.ProjectItems.OfType<ProjectItem>().FirstOrDefault(x => x.Name == "AssemblyInfo.cs");
            Name = assemblyInfo.FileCodeModel.CodeElements.OfType<CodeAttribute2>()
                .FirstOrDefault(x => x.Name == "AssemblyDefaultAlias")?.Value.Replace("\"",string.Empty);

            string fullPath = project.Properties.Item("FullPath").Value.ToString();
            string outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = project.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);

            BinInfo = new FileInfo(assemblyPath);
            var pdbPath = Path.Combine(outputDir, $"{BinInfo.Name.Replace(BinInfo.Extension, "")}.pdb");
            if (File.Exists(pdbPath))
            {
                PdbInfo = new FileInfo(pdbPath);
            }
            var RefsFiles = Directory.GetFiles(outputDir).Except(new string[] { BinInfo.FullName, PdbInfo?.FullName }).Select(x => new FileInfo(x)).Cast<FileSystemInfo>();
            var RefDirs = Directory.GetDirectories(outputDir).Select(x => new DirectoryInfo(x)).Cast<FileSystemInfo>();
            Refs = RefsFiles.Concat(RefDirs);
        }

    }
}
