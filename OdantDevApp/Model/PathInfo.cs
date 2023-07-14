namespace OdantDev.Model;
public struct PathInfo
{
    public PathInfo(string name, string path)
        : this()
    {
        Name = name;
        Path = path;
    }

    public string Path { get; set; }

    public string Name { get; set; }

    public override string ToString()
    {
        return $"{Name} ({Path})";
    }
}
