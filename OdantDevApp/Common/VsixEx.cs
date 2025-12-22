using System.Reflection;


/* Unmerged change from project 'OdantDevApp (net472)'
Before:
using Microsoft.Win32;
using NativeMethods;
After:
using Microsoft.Win32;

using NativeMethods;
*/
using Microsoft.Win32;

namespace OdantDev;

public static class VsixEx
{
    public static Bitness Platform => IntPtr.Size == 4 ? Bitness.x86 : Bitness.x64;

    public static DirectoryInfo VsixPath { get; } = ProcessEx.CurrentExecutingFolder();

    public static List<IntPtr> LoadServerLibraries(string odaPath, Bitness bitness, params string[] libPaths)
    {
        var serverPath = Path.Combine(odaPath, "server", bitness.ToString());
        var output = new List<IntPtr>();
        foreach (var path in libPaths)
        {
            var assembly = NativeMethods.WinApi.LoadLibraryEx(Path.Combine(serverPath, path), IntPtr.Zero, 8U);
            output.Add(assembly);
        }
        return output;
    }

    public static List<Assembly> LoadClientLibraries(string path, params string[] libPaths)
    {
        return libPaths.Select(libPath => Assembly.LoadFrom(Path.Combine(path, libPath))).ToList();
    }

    public static DirectoryInfo? LastOdaFolder { get; } = LastOdaPath()?.Directory;

    public static FileInfo? LastOdaPath()
    {
        string[] strArray = ["oda", "odant", "Applications\\ODA.exe"];
        foreach (var name1 in strArray)
        {
            try
            {
                var registryKey1 = Registry.ClassesRoot.OpenSubKey(name1);
                if (registryKey1 == null)
                {
                    continue;
                }

                const string name2 = "shell\\open\\command";
                var registryKey2 = registryKey1.OpenSubKey(name2);
                var obj = registryKey2?.GetValue(string.Empty);
                if (obj == null)
                {
                    continue;
                }

                registryKey1.Close();
                registryKey2.Close();
                var str1 = obj.ToString();
                var str2 = str1.Substring(str1.IndexOf("\"") + 1);
                return new FileInfo(str2.Substring(0, str2.IndexOf("\"")));
            }
            catch
            {
            }
        }
        return null;
    }
}
