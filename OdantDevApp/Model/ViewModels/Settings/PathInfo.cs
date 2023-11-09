namespace OdantDevApp.Model.ViewModels.Settings;
public struct PathInfo
{
    public PathInfo() { }
    public PathInfo(string name, string path) : this()
    {
        Name = name;
        Path = path;
    }
    public string Path { get; set; }

    public string Name { get; set; }

    public override string ToString() => $"{Name} ({Path})";
}
