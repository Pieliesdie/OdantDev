using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;

using OdantDev.Model;

namespace SharedOdantDev.Model;
public class RepoGroupViewModel : RepoBaseViewModel
{
    public RepoGroupViewModel(BaseGitItem item, BaseGitItem parent, bool loadProjects, ILogger logger = null) : base(item, parent, logger)
    {
        LoadProjects = loadProjects;
    }

    public override string Name => Item?.Name;
    public override bool HasModule => false;

    public virtual bool LoadProjects { get; set; }

    public override async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
    {
        if (GitClient.Client == null)
        {
            return Enumerable.Empty<RepoBaseViewModel>();
        }

        try
        {
            var children = new List<RepoBaseViewModel>();
            int id = ((Group)Item.Object).Id;
            IList<Group> groups = await GitClient.Client.Groups.GetSubgroupsAsync(id);
            if (groups != null)
            {
                foreach (Group innerGroup in groups)
                {
                    var newItem = new GroupItem(innerGroup);
                    children.Add(new RepoGroupViewModel(newItem, Item, LoadProjects, logger));
                }
            }

            if (!LoadProjects)
            {
                return children;
            }

            IList<Project> projects = await GitClient.Client.Groups.GetProjectsAsync(id);

            if (projects == null) { return children; }

            foreach (Project project in projects)
            {
                var newItem = new ProjectItem(project);
                children.Add(new RepoProjectViewModel(newItem, Item, logger));
            }

            return children;
        }
        catch(Exception ex)
        {
            return Enumerable.Empty<RepoBaseViewModel>();
        }
    }
}
