namespace OdantDev.Model;
public class PathInfo
{
    public PathInfo()
    {

    }
    public PathInfo(string name, string path)
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
