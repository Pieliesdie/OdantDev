using oda;
using System;
using System.IO;

namespace OdantDev.Model
{
    public partial class VisualStudioIntegration
    {
        public class BuildInfo : IDisposable
        {
            public BuildInfo(string name, Dir remoteDir, DirectoryInfo localDir)
            {
                Name = name;
                this.isBuildSuccess = isBuildSuccess;
                RemoteDir = remoteDir;
                LocalDir = localDir;
            }
            public string Name { get; set; }
            public bool isBuildSuccess { get; set; }
            public Dir RemoteDir { get; set; }
            public DirectoryInfo LocalDir { get; set; }

            public void Dispose()
            {
                RemoteDir.Dispose();
            }
        }
    }
}
