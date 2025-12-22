using GitLabApiClient.Models.Groups.Responses;
using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoGroup : RepoBase
{
    public RepoGroup(GroupItem? item, BaseGitItem? parent, bool loadProjects, ILogger? logger = null)
        : base(item, parent, logger)
    {
        LoadProjects = loadProjects;
    }
    public override bool HasModule => false;
    public virtual bool LoadProjects { get; set; }

    public override async Task<IEnumerable<RepoBase>> GetChildrenAsync()
    {
        if (GitClient.Client == null || Item == null)
        {
            return [];
        }

        try
        {
            var children = new List<RepoBase>();
            var id = ((Group)Item.Object).Id;
            var groups = await GitClient.Client.Groups.GetSubgroupsAsync(id);
            if (groups != null)
            {
                var innerGroupChildren = groups
                    .Select(innerGroup => new GroupItem(innerGroup))
                    .Select(newItem => new RepoGroup(newItem, Item, LoadProjects, Logger));
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
                .Select(newItem => new RepoProject(newItem, Item, Logger));

            children.AddRange(projectChildren);

            return children;
        }
        catch
        {
            return [];
        }
    }
}
