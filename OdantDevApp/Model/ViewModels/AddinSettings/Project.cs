using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OdantDev;

namespace OdantDevApp.Model.ViewModels;

public struct Project(string name, string fullId, string domainName, DateTime openTime, BitmapSource? icon = null)
{
    private ImageSource? icon;
    public string? IconBase64 { get; set; } = icon?.ToBase64String();
    public ImageSource? Icon => icon ??= IconBase64?.FromBase64String();
    public bool HasIcon => Icon is not null;
    public string Name { get; set; } = name;
    public string FullId { get; set; } = fullId;
    public string HostName { get; set; } = domainName;
    public DateTime OpenTime { get; set; } = openTime;

    public override bool Equals(object obj)
    {
        return obj is Project project &&
               FullId == project.FullId;
    }

    public override int GetHashCode()
    {
        return -191063783 + EqualityComparer<string>.Default.GetHashCode(FullId);
    }
}