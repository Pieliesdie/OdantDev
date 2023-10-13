using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OdantDev.Model;
using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoRootViewModel : RepoGroupViewModel
{
    public override bool HasModule => false;
    public RepoRootViewModel(RootItem item, bool loadProjects, ILogger logger = null)
        : base(null, null, loadProjects, logger) { this.Item = item; }

    public override async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
    {
        if (GitClient.Client == null) { return Enumerable.Empty<RepoBaseViewModel>(); }

        var children = new List<RepoBaseViewModel>();
        var groups = await GitClient.Client.Groups.GetAsync();

        if (groups != null)
        {
            var groupChildren = groups
                .Where(x => x.ParentId == null)
                .Select(group => new GroupItem(group))
                .Select(newItem => new RepoGroupViewModel(newItem, Item, LoadProjects, Logger));
            children.AddRange(groupChildren);
        }

        if (!LoadProjects) { return children; }

        var projects = await GitClient.Client.Projects.GetAsync();
        if (projects == null) { return children; }

        var notGroupChildren = 
            projects
            .Where(x => x.Namespace.Kind != "group")
            .Select(project => new ProjectItem(project))
            .Select(newItem => new RepoProjectViewModel(newItem, Item, Logger));

        children.AddRange(notGroupChildren);
        return children;
    }
}
