using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git.GitItems;
public class RootItem(string name) : BaseGitItem
{
    public override string Name { get; } = name;

    public override object Object { get; }

    public override bool HasModule => false;

    public override ImageSource Icon => PredefinedImages.FolderImage;
}
