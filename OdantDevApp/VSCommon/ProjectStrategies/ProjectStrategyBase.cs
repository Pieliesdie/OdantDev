using System.Diagnostics;
using EnvDTE80;
using VSLangProj;
using File = System.IO.File;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public abstract class ProjectStrategyBase(ILogger? logger) : IProjectStrategy
{
    protected static bool IsReferenceOutdated(
        string referenceName,
        string currentPath,
        DirectoryInfo referencesFolder,
        HashSet<string> references,
        bool force
    )
    {
        try
        {
            return IsReferenceOutdatedCore(referenceName, currentPath, referencesFolder, references, force);
        }
        catch
        {
            return false;
        }
    }

    protected static bool IsReferenceOutdatedCore(
        string referenceName,
        string currentPath,
        DirectoryInfo referencesFolder,
        HashSet<string> references,
        bool force
    )
    {
        var isInUpdateList = references.Contains(referenceName);
        if (!isInUpdateList)
        {
            return false;
        }

        var newPath = Path.Combine(referencesFolder.FullName, referenceName);
        var isNewPathExists = File.Exists(newPath);
        if (!isNewPathExists)
        {
            return false;
        }

        var isSameFileVersion = GetFileVersion(newPath) == GetFileVersion(currentPath);
        var exists = File.Exists(currentPath);
        return !exists || !isSameFileVersion || force;
    }

    protected static string GetFileVersion(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(path);
        return versionInfo.FileVersion;
    }

    protected void UpdateAssemblyInfoFile(Project project, Item item, DirectoryInfo templatesFolder)
    {
        var directory = new FileInfo(project.FullName).DirectoryName ?? string.Empty;
        var assemblyFile = Path.Combine(directory, "AssemblyInfo.cs");

        var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs");
        if (assemblyInfo == null || !File.Exists(assemblyFile))
        {
            if (File.Exists(assemblyFile))
            {
                File.Delete(assemblyFile);
            }

            var templatePath = Path.Combine(templatesFolder.FullName, @"Templates\AssemblyInfo.cs");
            assemblyInfo = project.ProjectItems.AddFromFileCopy(templatePath);
        }

        SetAttribute(assemblyInfo, "AssemblyTitle", $"{item.Name}-{item.Id}");
        SetAttribute(assemblyInfo, "AssemblyDescription", item.Hint ?? string.Empty);
        SetAttribute(assemblyInfo, "AssemblyCopyright", $"ООО «Инфостандарт» © 2012 — {DateTime.Now.Year}");
        SetAttribute(assemblyInfo, "AssemblyDefaultAlias", $"{item.Name}");
        SetAttribute(assemblyInfo, "AssemblyCompany", "Infostandart");
        SetAttribute(assemblyInfo, "AssemblyTrademark", "www.infostandart.com");

        SaveProjectItem(assemblyInfo);
    }

    protected void SaveProjectItem(ProjectItem item)
    {
        try
        {
            if (!item.IsOpen)
            {
                item.Open();
            }

            item.Save();
        }
        catch (Exception err)
        {
            logger?.LogError(err, "Error in SaveProjectItem: {Error}", err.Message);
        }
    }

    protected void SetAttribute(ProjectItem item, string name, string value)
    {
        var attr = FindCodeAttribute(item, name);
        if (attr == null)
        {
            item.FileCodeModel?.AddAttribute(name, $"\"{value}\"");
        }
        else
        {
            attr.Value = $"\"{value}\"";
        }
    }

    protected ProjectItem? FindProjectItem(Project project, string name)
    {
        try
        {
            var projectItem = project.ProjectItems?.OfType<ProjectItem>()?.FirstOrDefault(x => x.Name == name);
            (projectItem?.FileCodeModel as FileCodeModel2)?.Synchronize();
            return projectItem;
        }
        catch (Exception err)
        {
            logger?.LogError(err, "Error in FindProjectItem: {Error}", err.Message);
            return null;
        }
    }

    protected CodeAttribute2? FindCodeAttribute(ProjectItem? projectItem, string name)
    {
        try
        {
            return projectItem?.FileCodeModel?.CodeElements?.OfType<CodeAttribute2>()
                ?.FirstOrDefault(x => x.Name == name);
        }
        catch (Exception err)
        {
            logger?.LogError(err, "Error in FindCodeAttribute: {Error}", err.Message);
            return null;
        }
    }

    public abstract bool IsMatch(Project project);

    public abstract bool InitProject(Project project, StructureItem item, DirectoryInfo odaFolder,
        DirectoryInfo templatesFolder);

    public abstract bool UpdateReferences(VSProject vsProj, DirectoryInfo referencesFolder,
        IEnumerable<string> references, bool force);

    public virtual Version GetVersion(Project project)
    {
        var assemblyInfo = FindProjectItem(project, "AssemblyInfo.cs");
        if (assemblyInfo == null)
        {
            throw new FileNotFoundException("AssemblyInfo.cs not found");
        }

        var val = FindCodeAttribute(assemblyInfo, "AssemblyVersion")?.Value?.Replace("\"", string.Empty);
        if (val == null || !Version.TryParse(val, out var av))
        {
            return new Version("1.0.0.0");
        }

        return av;
    }

    public virtual bool IncreaseVersion(Project project)
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

            return true;
        }
        catch (Exception err)
        {
            logger?.LogCritical(err, "IncreaseVersion error: {Error}", err.Message);
            return false;
        }
    }
}