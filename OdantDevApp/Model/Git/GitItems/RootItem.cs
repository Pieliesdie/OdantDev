using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

    protected override string ImageCode => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z";
}
