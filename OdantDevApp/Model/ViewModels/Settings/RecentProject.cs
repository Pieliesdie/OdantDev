using System.Windows.Media.Imaging;
using System.Xml.Serialization;

namespace OdantDevApp.Model.ViewModels.Settings;

[XmlType(TypeName = "Project")]
public struct RecentProject(string name, string fullId, string domainName, DateTime openTime, BitmapSource? icon = null)
{
    public string? IconBase64 { get; set; } = icon?.ToBase64String();
    public ImageSource? Icon => field ??= IconBase64?.FromBase64String();
    public bool HasIcon => Icon is not null;
    public string Name { get; set; } = name;
    public string FullId { get; set; } = fullId;
    public string HostName { get; set; } = domainName;
    public DateTime OpenTime { get; set; } = openTime;

    public override bool Equals(object? obj) => obj is RecentProject project && FullId == project.FullId;

    public override int GetHashCode() => -191063783 + EqualityComparer<string>.Default.GetHashCode(FullId);

    public static bool operator ==(RecentProject left, RecentProject right) => left.Equals(right);

    public static bool operator !=(RecentProject left, RecentProject right) => !(left == right);
}