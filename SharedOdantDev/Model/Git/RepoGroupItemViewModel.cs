using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GitLabApiClient.Models.Groups.Responses;
using GitLabApiClient.Models.Projects.Responses;
using OdantDev.Model;

namespace SharedOdantDev.Model
{
    public class RepoGroupViewModel : RepoBaseViewModel
    {
        public RepoGroupViewModel() { }

        public RepoGroupViewModel(GroupItem item, bool lazyLoad, BaseGitItem parent, bool loadProjects, ILogger logger = null)
        {
            _isLazyLoading = lazyLoad;
            _logger = logger;
            Item = item;
            Parent = parent;
            LoadProjects = loadProjects;

            _ = InitChildrenAsync();
        }

        public override string Name => Item?.Name;
        public override bool HasModule => false;

        public virtual bool LoadProjects { get; set; }

        public virtual async Task InitChildrenAsync()
        {
            Task<IEnumerable<RepoBaseViewModel>> task = GetChildrenAsync();
            if (task == await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))))
            {
                Children = await task;
            }
            else
            {
                _logger?.Info($"Timeout when getting children for {Item}");
                Children = null;
            }
        }

        public virtual async Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync()
        {
            var children = new List<RepoBaseViewModel>();

            if (GitClient.Client != null)
            {
                int id = ((Group)Item.Object).Id;
                IList<Group> groups = await GitClient.Client.Groups.GetSubgroupsAsync(id); ;
                if (groups != null)
                {
                    foreach (Group innerGroup in groups)
                    {
                        var newItem = new GroupItem(innerGroup);
                        children.Add(new RepoGroupViewModel(newItem, _isLazyLoading, Item, LoadProjects, _logger));
                    }
                }

                if (LoadProjects)
                {
                    IList<Project> projects = await GitClient.Client.Groups.GetProjectsAsync(id);
                    ;
                    if (projects != null)
                    {
                        foreach (Project project in projects)
                        {
                            var newItem = new ProjectItem(project);
                            children.Add(new RepoProjectViewModel(newItem, _isLazyLoading, Item, _logger));
                        }
                    }
                }
            }

            return children;
        }
    }
}
