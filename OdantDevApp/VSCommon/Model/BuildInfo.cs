using System;
using System.IO;

using oda;

namespace OdantDev.Model;
public sealed partial class VisualStudioIntegration
{
    struct BuildInfo
    {
        public BuildInfo(string name, Dir remoteDir, DirectoryInfo localDir)
        {
            Name = name;
            RemoteDir = remoteDir;
            LocalDir = localDir;
        }
        public string Name { get; set; }
        public Dir RemoteDir { get; set; }
        public DirectoryInfo LocalDir { get; set; }
    }
}
