using EnvDTE;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public interface IProjectStrategy
{
    bool IsMatch(Project project);
    void InitProject(Project project, StructureItem item, DirectoryInfo odaFolder, string templatesPath);
    Version GetVersion(Project project);
    bool IncreaseVersion(Project project);
}
