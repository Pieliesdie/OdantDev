using System.Windows.Media;

using SharedOdanDev.OdaOverride;

namespace SharedOdantDev.Model;
public class RootItem : BaseGitItem
{
    public RootItem(string name)
    {
        Name = name;
    }

    public override string Name { get; }

    public override object Object { get; }

    public override bool HasModule => false;

    public override ImageSource Icon => PredefinedImages.FolderImage;
}
