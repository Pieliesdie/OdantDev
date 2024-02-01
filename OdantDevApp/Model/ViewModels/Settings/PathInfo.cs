namespace OdantDevApp.Model.ViewModels.Settings;

public record struct PathInfo(string Name, string Path)
{
    public override string ToString() => $"{Name} ({Path})";
}
