using OdantDev.Model;
using System.Collections.Generic;

namespace SharedOdantDev.Model
{
    public class RepoProjectViewModel : RepoBaseViewModel
    {
        public override string Name => Item == null ? GitClient.Client?.HostUrl : Item.Name;

        private RepoProjectViewModel() { }

        public RepoProjectViewModel(ProjectItem item, bool lazyLoad, BaseGitItem parent, ILogger logger = null)   
        {
            _isLazyLoading = lazyLoad;
            _logger = logger;
            Item = item;
            Parent = parent;
        }

        public override bool HasModule => Item.HasModule;

        public override IEnumerable<RepoBaseViewModel> Children => null;
    }
}

// public virtual async Task<IEnumerable<BaseViewModel<T>>> GetChildrenAsync(T item, bool lazyLoad)
// {
//     var children = new List<RepoProjectViewModel<T>>();
//
//     if (GitClient.Client != null)
//     {
//         if (item == null)
//         {
//             IList<Group> groups = await GitClient.Client.Groups.GetAsync(); ;
//             if (groups != null)
//             {
//                 foreach (Group group in groups)
//                 {
//                     if (group.ParentId != null)
//                         continue;
//
//                     var newItem = new GroupItem(group);
//
//                     children.Add(new RepoProjectViewModel<T>(newItem as T, lazyLoad, item, _logger));
//                 }
//             }
//         }
//         else if (item.Object is Group group)
//         {
//             IList<Project> projects = await GitClient.Client.Groups.GetProjectsAsync(group.Id); ;
//             IList<Group> groups = await GitClient.Client.Groups.GetSubgroupsAsync(group.Id); ;
//             if (groups != null)
//             {
//                 foreach (Group innerGroup in groups)
//                 {
//                     var newItem = new GroupItem(innerGroup);
//                     children.Add(new RepoProjectViewModel<T>(newItem as T, lazyLoad, item, _logger));
//                 }
//             }
//             if (projects != null)
//             {
//                 foreach (Project project in projects)
//                 {
//                     var newItem = new ProjectItem(project);
//                     children.Add(new RepoProjectViewModel<T>(newItem as T, lazyLoad, item, _logger));
//                 }
//             }
//         }
//     }
//
//     return children;
// }
