using EnvDTE;
using EnvDTE80;
using OdantDev.Model;
using VSLangProj;
using File = System.IO.File;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public class LegacyProjectStrategy(ILogger? logger) : IProjectStrategy
{
    public bool IsMatch(Project project) => true; // Fallback

    public void InitProject(Project project, StructureItem item, DirectoryInfo odaFolder, string templatesPath)
    {
        var startProgram = Environment.Is64BitOperatingSystem ? "ODA.exe" : "oda.wrapper32.exe";
        var directory = new FileInfo(project.FullName).DirectoryName ?? string.Empty;
        var assemblyFile = Path.Combine(directory, "AssemblyInfo.cs");

        project.Name = $"{item.Name}-{item.Id}";

        try
        {
            var props = project.ConfigurationManager.ActiveConfiguration.Properties;
            props.Item("StartAction").Value = prjStartAction.prjStartActionProgram;
            props.Item("StartProgram").Value = Path.Combine(odaFolder.FullName, startProgram);
            props.Item("StartArguments").Value = "debug";

            project.Properties.Item("AssemblyName").Value = project.Name;
            project.Properties.Item("ReferencePath").Value = odaFolder.FullName;
        }
        catch (Exception e)
        {
            logger?.Error($"Legacy Init props error: {e.Message}");
        }


        var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs");
        if (assemblyInfo == null || !File.Exists(assemblyFile))
        {
            if (File.Exists(assemblyFile)) File.Delete(assemblyFile);

            var templatePath = Path.Combine(templatesPath, @"Templates\AssemblyInfo.cs");
            assemblyInfo = project.ProjectItems.AddFromFileCopy(templatePath);
        }

        SetAttribute(assemblyInfo, "AssemblyTitle", $"{item.Name}-{item.Id}");
        SetAttribute(assemblyInfo, "AssemblyDescription", item.Hint ?? string.Empty);
        SetAttribute(assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
        SetAttribute(assemblyInfo, "AssemblyDefaultAlias", $"{item.Name}");
        SetAttribute(assemblyInfo, "AssemblyCompany", "Infostandart");
        SetAttribute(assemblyInfo, "AssemblyTrademark", "www.infostandart.com");

        IncreaseVersion(project);
        SaveProjectItem(assemblyInfo);
    }

    public Version GetVersion(Project project)
    {
        var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs");
        if (assemblyInfo == null) throw new FileNotFoundException("AssemblyInfo.cs not found");

        var val = FindCodeAttribute(assemblyInfo, "AssemblyVersion")?.Value?.Replace("\"", string.Empty);
        return new Version(val ?? "1.0.0.0");
    }

    public bool IncreaseVersion(Project project)
    {
        try
        {
            var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs")
                               ?? throw new NullReferenceException($"Missing AssemblyInfo.cs in {project.Name}");

            var versionAttr = FindCodeAttribute(assemblyInfo, "AssemblyVersion");

            if (versionAttr == null)
            {
                assemblyInfo.FileCodeModel?.AddAttribute("AssemblyVersion", $"\"{Utils.Version}\"");
            }
            else
            {
                var currentVersion = Version.Parse(versionAttr.Value.Replace("\"", string.Empty));
                var currentOdantVersion = Version.Parse(Utils.Version);
                versionAttr.Value =
                    $"\"{Utils.MajorVersion}.{Utils.ShortVersion}.{Math.Max(currentOdantVersion.Revision, currentVersion.Revision + 1)}\"";
            }

            SaveProjectItem(assemblyInfo);
            return true;
        }
        catch (Exception ex)
        {
            logger?.Error($"Legacy IncreaseVersion error: {ex.Message}");
            return false;
        }
    }

    private static ProjectItem? FindProjectItem(Project project, string name)
    {
        var projectItem = project.ProjectItems?.OfType<ProjectItem>()?.FirstOrDefault(x => x.Name == name);
        (projectItem?.FileCodeModel as FileCodeModel2)?.Synchronize();
        return projectItem;
    }

    private static CodeAttribute2? FindCodeAttribute(ProjectItem projectItem, string name)
    {
        return projectItem.FileCodeModel?.CodeElements?.OfType<CodeAttribute2>()?.FirstOrDefault(x => x.Name == name);
    }

    private static void SetAttribute(ProjectItem item, string name, string value)
    {
        var attr = FindCodeAttribute(item, name);
        if (attr == null) item.FileCodeModel?.AddAttribute(name, $"\"{value}\"");
        else attr.Value = $"\"{value}\"";
    }

    private static void SaveProjectItem(ProjectItem item)
    {
        if (!item.IsOpen) item.Open();
        item.Save();
    }
}