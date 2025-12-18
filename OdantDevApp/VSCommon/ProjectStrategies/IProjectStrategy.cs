using EnvDTE;
using VSLangProj;

namespace OdantDevApp.VSCommon.ProjectStrategies;

public interface IProjectStrategy
{
    bool IsMatch(Project project);
    bool InitProject(Project project, StructureItem item, DirectoryInfo odaFolder, DirectoryInfo templatesFolder);
    Version GetVersion(Project project);
    bool IncreaseVersion(Project project);
    bool UpdateReferences(VSProject vsProj, DirectoryInfo referencesFolder, IEnumerable<string> references, bool force);
}