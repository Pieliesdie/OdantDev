using System.Collections.Concurrent;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using SharedOdanDev.OdaOverride;

namespace SharedOdantDev.Model;
public abstract class BaseGitItem
{
    public abstract string Name { get; }

    public abstract object Object { get; }

    public virtual string FullPath { get; protected set; }

    public virtual ImageSource Icon => PredefinedImages.FolderImage;

    public virtual bool HasModule { get; set; }
}
