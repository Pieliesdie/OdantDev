using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GitLabApiClient.Models.Groups.Responses;

using OdantDev.Model;
using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoGroupViewModel : RepoBaseViewModel
{
    public RepoGroupViewModel(GroupItem item, BaseGitItem parent, bool loadProjects, ILogger? logger = null)
        : base(item, parent, logger)
    {
        LoadProjects = loadProjects;
    }
    public override bool HasModule => false;
    public virtual bool LoadProjects { get; set; }

    public override async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
    {
        if (GitClient.Client == null || Item == null)
        {
            return Enumerable.Empty<RepoBaseViewModel>();
        }

        try
        {
            var children = new List<RepoBaseViewModel>();
            var id = ((Group)Item.Object).Id;
            var groups = await GitClient.Client.Groups.GetSubgroupsAsync(id);
            if (groups != null)
            {
                var innerGroupChildren = groups
                    .Select(innerGroup => new GroupItem(innerGroup))
                    .Select(newItem => new RepoGroupViewModel(newItem, Item, LoadProjects, Logger));
                children.AddRange(innerGroupChildren);
            }

            if (!LoadProjects)
            {
                return children;
            }

            var projects = await GitClient.Client.Groups.GetProjectsAsync(id);

            if (projects == null) { return children; }

            var projectChildren = projects
                .Select(project => new ProjectItem(project))
                .Select(newItem => new RepoProjectViewModel(newItem, Item, Logger));

            children.AddRange(projectChildren);

            return children;
        }
        catch
        {
            return Enumerable.Empty<RepoBaseViewModel>();
        }
    }
}
