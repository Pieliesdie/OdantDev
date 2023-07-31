using System;
using System.Windows.Media;

using GitLabApiClient.Models.Projects.Responses;

using MaterialDesignThemes.Wpf;

using oda;

using OdantDev;

using SharedOdanDev.OdaOverride;

namespace SharedOdantDev.Model;
public class ProjectItem : BaseGitItem
{
    private readonly Project _project;

    public ProjectItem(Project project)
    {
        _project = project;
    }

    public override string Name => $"{_project.Name} ({_project.Path})";

    public override object Object => _project;

    public override bool HasModule => true;

    public override ImageSource Icon => PredefinedImages.GitProject;
}
