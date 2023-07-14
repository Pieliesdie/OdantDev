using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace oda.OdaOverride;

public static class CommonEx
{
    public static DirectoryInfo DefaultSettingsFolder => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODA", "AddinSettings"));
    public static Connection Connection { get; set; }
}
