using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoRoot : RepoGroup
{
    public override bool HasModule => false;

    public RepoRoot(RootItem item, bool loadProjects, ILogger? logger = null)
        : base(null, null, loadProjects, logger)
    {
        Item = item;
    }

    public override async Task<IEnumerable<RepoBase>> GetChildrenAsync()
    {
        if (GitClient.Client == null) { return Enumerable.Empty<RepoBase>(); }

        var children = new List<RepoBase>();
        var groups = await GitClient.Client.Groups.GetAsync();

        if (groups != null)
        {
            var groupChildren = groups
                .Where(x => x.ParentId == null)
                .Select(group => new GroupItem(group))
                .Select(newItem => new RepoGroup(newItem, Item, LoadProjects, Logger));
            children.AddRange(groupChildren);
        }

        if (!LoadProjects) { return children; }

        var projects = await GitClient.Client.Projects.GetAsync();
        if (projects == null) { return children; }

        var notGroupChildren = 
            projects
            .Where(x => x.Namespace.Kind != "group")
            .Select(project => new ProjectItem(project))
            .Select(newItem => new RepoProject(newItem, Item, Logger));

        children.AddRange(notGroupChildren);
        return children;
    }
}
