using System.ComponentModel;

using odaServer;

namespace OdaOverride;

public class DomainSystem : Domain
{
    [Browsable(false)]
    public override int SortIndex => -20;

    public override string TypeLabel => "Системный домен";

    public override int ImageIndex
    {
        get
        {
            if (Class == null || Class.Icon == null)
            {
                return Images.GetImageIndex(Icons.Lock);
            }

            return Class.ImageIndex;
        }
    }

    public override bool IsAccessible => AccessLevel == AccessLevel.Admin;

    public override bool IsVisible => AccessLevel == AccessLevel.Admin;

    public DomainSystem(ODADomain d, Item own)
        : base(d, own)
    {
    }

    protected override string additionToString()
    {
        return string.Empty;
    }
}
