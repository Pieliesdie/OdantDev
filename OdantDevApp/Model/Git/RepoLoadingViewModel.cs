using System.Windows.Media;
using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git;

public class RepoLoadingViewModel : OdantDevApp.Model.Git.RepoBaseViewModel
{
    public RepoLoadingViewModel() : base(null, null, null) { }
    public override string Name => "Loading...";

    public override ImageSource Icon => PredefinedImages.LoadImage;
}
