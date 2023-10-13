using System.Windows.Media;
using GitLabApiClient.Models.Groups.Responses;
using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git.GitItems;
public class GroupItem : BaseGitItem
{
    private readonly Group group;

    public GroupItem(Group group)
    {
        this.group = group;
        FullPath = this.group.FullPath;
    }

    public override string Name => group.Name;

    public override object Object => group;

    public override bool HasModule => false;

    public override ImageSource Icon => PredefinedImages.FolderImage;
}
