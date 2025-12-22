namespace OdantDevApp.VSCommon;

public sealed partial class VisualStudioIntegration
{
    private struct BuildInfo(string name, Dir remoteDir, DirectoryInfo localDir, Item sourceItem)
    {
        public Item SourceItem { get; set; } = sourceItem;
        public string Name { get; set; } = name;
        public Dir RemoteDir { get; set; } = remoteDir;
        public DirectoryInfo LocalDir { get; set; } = localDir;
    }
}
