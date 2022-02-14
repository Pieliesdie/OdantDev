using oda;
using System.IO;

namespace OdantDev.Model
{
    public partial class OdaAddinModel
    {
        public class BuildInfo
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
        }
    }
}
