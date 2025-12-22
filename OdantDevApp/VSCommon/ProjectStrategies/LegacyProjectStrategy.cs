using System.Reflection;
using EnvDTE;
using VSLangProj;
using VSLangProj80;
using File = System.IO.File;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public class LegacyProjectStrategy(ILogger? logger) : ProjectStrategyBase(logger)
{
    private readonly ILogger? logger = logger;

    private static Configuration ConfigureConfiguration(Configuration configuration, DirectoryInfo odaFolder)
    {
        var startProgram = Environment.Is64BitOperatingSystem ? "ODA.exe" : "oda.wrapper32.exe";

        configuration.Properties.Item("StartAction").Value = prjStartAction.prjStartActionProgram;
        configuration.Properties.Item("StartProgram").Value = Path.Combine(odaFolder.FullName, startProgram);
        configuration.Properties.Item("StartArguments").Value = "debug";
        return configuration;
    }

    private static Configuration EnsureConfiguration(Project project, string configName, string baseConfigName)
    {
        var rowNames = ((object[])project.ConfigurationManager.ConfigurationRowNames).Cast<string>();

        if (!rowNames.Contains(configName))
        {
            project.ConfigurationManager.AddConfigurationRow(configName, baseConfigName, true);
        }

        return project.ConfigurationManager.Item(configName, "Any CPU");
    }

    private bool UpdateAssemblyReferencesCore(
        VSProject vsProj,
        DirectoryInfo referencesFolder,
        HashSet<string> references,
        bool force
    )
    {
        foreach (var reference in vsProj.References.OfType<Reference>().ToList())
        {
            try
            {
                UpdateAssemblyReferenceCore(reference, vsProj, referencesFolder, references, force);
            }
            catch (Exception e)
            {
                logger?.LogCritical(e, "Error while update assembly references : {Error}", e.Message);
            }
        }

        return true;
    }

    private bool UpdateAssemblyReferenceCore(
        Reference reference,
        VSProject vsProj,
        DirectoryInfo referencesFolder,
        HashSet<string> references, bool force
    )
    {
        var assemblyName = new AssemblyName(reference.Name);
        var referenceName = $"{assemblyName.Name}.dll";

        if (!IsReferenceOutdated(referenceName, reference.Path, referencesFolder, references, force))
        {
            return true;
        }

        var newReferencePath = Path.Combine(referencesFolder.FullName, referenceName);
        if (!File.Exists(newReferencePath))
        {
            return false;
        }

        reference.Remove();

        var newReference = (Reference3)vsProj.References.Add(newReferencePath);
        newReference.CopyLocal = false;
        newReference.SpecificVersion = false;

        logger?.LogInformation("'{Reference}' is updated", newReference.Name);
        return true;
    }

    private bool InitProjectCore(
        Project project,
        StructureItem item,
        DirectoryInfo odaFolder,
        DirectoryInfo templatesFolder
    )
    {
        project.Name = $"{item.Name}-{item.Id}";

        var debugConfig = EnsureConfiguration(project, "Debug", string.Empty);
        ConfigureConfiguration(debugConfig, odaFolder);

        var releaseConfig = EnsureConfiguration(project, "Release", "Debug");
        ConfigureConfiguration(releaseConfig, odaFolder);

        project.Properties.Item("AssemblyName").Value = project.Name;
        project.Properties.Item("ReferencePath").Value = odaFolder.FullName;

        UpdateAssemblyInfoFile(project, item, templatesFolder);
        return true;
    }

    public override bool IsMatch(Project project) => true;

    public override bool InitProject(
        Project project,
        StructureItem item,
        DirectoryInfo odaFolder,
        DirectoryInfo templatesFolder
    )
    {
        try
        {
            return InitProjectCore(project, item, odaFolder, templatesFolder);
        }
        catch (Exception e)
        {
            logger?.LogCritical(e, "Error while initializing project : {Error}", e.Message);
            return false;
        }
    }

    public override bool UpdateReferences(VSProject vsProj, DirectoryInfo referencesFolder,
        IEnumerable<string> references, bool force)
    {
        try
        {
            return UpdateAssemblyReferencesCore(vsProj, referencesFolder, references.ToHashSet(), force);
        }
        catch (Exception e)
        {
            logger?.LogCritical(e, "Error while update assembly references : {Error}", e.Message);
            return false;
        }
    }
}