using System.Collections.Generic;

using OdantDev.Model;

namespace SharedOdantDev.Model;
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
