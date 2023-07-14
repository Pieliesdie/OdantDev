using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GitLabApiClient.Models.Groups.Responses;

using OdantDev.Model;

namespace SharedOdantDev.Model;
public class RepoRootViewModel : RepoGroupViewModel
{
    private string name = new Uri(GitClient.Client?.HostUrl).Host;
    public override string Name => name;
    public override bool HasModule => false;
    public RepoRootViewModel(BaseGitItem item, bool loadProjects, ILogger logger = null) 
        : base(item, null, loadProjects, logger) {    }

    public override async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
    {
        if (GitClient.Client == null) { return Enumerable.Empty<RepoBaseViewModel>(); }

        var children = new List<RepoBaseViewModel>();
        IList<Group> groups = await GitClient.Client.Groups.GetAsync();

        if (groups != null)
        {
            foreach (Group group in groups.Where(x=> x.ParentId != null))
            {
                var newItem = new GroupItem(group);

                children.Add(new RepoGroupViewModel(newItem, Item, LoadProjects, logger));
            }
        }

        if (!LoadProjects) { return children; }

        var projects = await GitClient.Client.Projects.GetAsync();
        if (projects == null) { return children; }

        foreach (var project in projects.Where(x => x.Namespace.Kind != "group"))
        {
            var newItem = new ProjectItem(project);
            children.Add(new RepoProjectViewModel(newItem, Item, logger));
        }
        return children;
    }
}
