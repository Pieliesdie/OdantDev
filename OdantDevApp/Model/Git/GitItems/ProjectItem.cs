using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using GitLabApiClient.Models.Projects.Responses;

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

    protected override string ImageCode => "M17,18L12,15.82L7,18V5H17M17,3H7A2,2 0 0,0 5,5V21L12,18L19,21V5C19,3.89 18.1,3 17,3Z";
}
