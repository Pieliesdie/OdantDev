using System.Windows.Media;
using GitLabApiClient.Models.Projects.Responses;
using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git.GitItems;
public class ProjectItem(Project project) : BaseGitItem
{
    public override string Name => $"{project.Name} ({project.Path})";

    public override object Object => project;

    public override bool HasModule => true;

    public override ImageSource Icon => PredefinedImages.GitProject;
}
