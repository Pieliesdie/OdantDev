using System;
using System.IO;

using oda;

namespace OdantDev.Model
{
    public partial class VisualStudioIntegration
    {
        public class BuildInfo : IDisposable
        {
            public BuildInfo(string name, Dir remoteDir, DirectoryInfo localDir)
            {
                Name = name;
                RemoteDir = remoteDir;
                LocalDir = localDir;
            }
            public string Name { get; set; }
            public bool IsBuildSuccess { get; set; }
            public Dir RemoteDir { get; set; }
            public DirectoryInfo LocalDir { get; set; }

            public void Dispose()
            {
                //RemoteDir?.Dispose();
            }
        }
    }
}
