using System.Collections.Generic;
using System.Threading.Tasks;
using OdantDev.Model;
using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoProjectViewModel : RepoBaseViewModel
{
    public RepoProjectViewModel(ProjectItem item, BaseGitItem? parent, ILogger? logger = null) : base(item, parent, logger) { }

    public override bool HasModule => Item?.HasModule ?? false;
    protected override bool CanBeExpanded => false;

    public override Task<IEnumerable<RepoBaseViewModel>> GetChildrenAsync() => Task.FromResult<IEnumerable<RepoBaseViewModel>>(null);
}
