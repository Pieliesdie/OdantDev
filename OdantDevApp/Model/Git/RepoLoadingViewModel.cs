using System.Windows.Media;

using SharedOdanDev.OdaOverride;

namespace SharedOdantDev.Model;

public class RepoLoadingViewModel : RepoBaseViewModel
{
    public RepoLoadingViewModel() : base(null, null, null) { }
    public override string Name => "Loading...";

    public override ImageSource Icon => PredefinedImages.LoadImage;
}
