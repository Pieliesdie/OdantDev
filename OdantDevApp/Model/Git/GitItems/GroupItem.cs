using GitLabApiClient.Models.Groups.Responses;

namespace SharedOdantDev.Model;
public class GroupItem : BaseGitItem
{
    private readonly Group _group;

    public GroupItem(Group group)
    {
        _group = group;
        FullPath = _group.FullPath;
    }

    public override string Name => _group.Name;

    public override object Object => _group;

    public override bool HasModule => false;

    protected override string ImageCode => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z";
}
