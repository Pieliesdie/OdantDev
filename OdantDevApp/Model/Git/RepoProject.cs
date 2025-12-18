using OdantDevApp.Model.Git.GitItems;

namespace OdantDevApp.Model.Git;
public class RepoProject : RepoBase
{
    public RepoProject(ProjectItem item, BaseGitItem? parent, ILogger? logger = null) : base(item, parent, logger) { }

    public override bool HasModule => Item?.HasModule ?? false;
    protected override bool CanBeExpanded => false;

    public override Task<IEnumerable<RepoBase>> GetChildrenAsync() => Task.FromResult<IEnumerable<RepoBase>>(null);
}
