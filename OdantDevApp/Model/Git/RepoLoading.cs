using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git;

public class RepoLoading : RepoBase
{
    public RepoLoading() : base(null, null, null) { }
    public override string Name => "Loading...";

    public override ImageSource Icon => PredefinedImages.LoadImage;
}
