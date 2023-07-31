using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OdaOverride;

public static class Common
{
    public static DirectoryInfo DefaultSettingsFolder => Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ODA", "AddinSettings"));
}
