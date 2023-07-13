using System.Collections.Generic;
using System.Threading.Tasks;

using GitLabApiClient.Models.Groups.Responses;

using OdantDev.Model;

namespace SharedOdantDev.Model;
public class RepoRootViewModel : RepoGroupViewModel
{
    public override bool HasModule => false;

    public RepoRootViewModel() { }

    public RepoRootViewModel(BaseGitItem item, bool lazyLoad, bool loadProjects, ILogger logger = null)
    {
        Item = item;
        _isLazyLoading = lazyLoad;
        _logger = logger;
        LoadProjects = loadProjects;

        _ = InitChildrenAsync();
    }

    public override async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
    {
        var children = new List<RepoBaseViewModel>();

        if (GitClient.Client != null)
        {
            IList<Group> groups = await GitClient.Client.Groups.GetAsync();
            if (groups != null)
            {
                foreach (Group group in groups)
                {
                    if (group.ParentId != null)
                        continue;

                    var newItem = new GroupItem(group);

                    children.Add(new RepoGroupViewModel(newItem, _isLazyLoading, Item, LoadProjects, _logger));
                }
            }

            if (LoadProjects)
            {
                IList<GitLabApiClient.Models.Projects.Responses.Project> projects = await GitClient.Client.Projects.GetAsync();
                if (projects != null)
                {
                    foreach (GitLabApiClient.Models.Projects.Responses.Project project in projects)
                    {
                        if (project.Namespace.Kind == "group")
                            continue;

                        var newItem = new ProjectItem(project);
                        children.Add(new RepoProjectViewModel(newItem, _isLazyLoading, Item, _logger));
                    }
                }
            }
        }

        return children;
    }
}
