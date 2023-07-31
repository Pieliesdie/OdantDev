using System.Windows.Media;

using GitLabApiClient.Models.Groups.Responses;

using SharedOdanDev.OdaOverride;

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

    public override ImageSource Icon
    {
        get
        {
            return PredefinedImages.FolderImage;
        }
    }
}
