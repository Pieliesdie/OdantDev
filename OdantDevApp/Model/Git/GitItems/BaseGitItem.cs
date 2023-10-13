using System.Windows.Media;
using SharedOdanDev.OdaOverride;

namespace OdantDevApp.Model.Git.GitItems;
public abstract class BaseGitItem
{
    public abstract string Name { get; }

    public abstract object Object { get; }

    public virtual string FullPath { get; protected set; }

    public virtual ImageSource Icon => PredefinedImages.FolderImage;

    public virtual bool HasModule { get; set; }
}
