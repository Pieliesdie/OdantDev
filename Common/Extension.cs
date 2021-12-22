using EnvDTE80;
using Microsoft.Win32;
using oda;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace OdantDev
{
    public static class Extension
    {
        public static bool Not(this bool boolean) => !boolean;
        public static BitmapImage ConvertToBitmapImage(this Bitmap src)
        {
            if (src == null) return null;
            MemoryStream ms = new MemoryStream();
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        public static List<IntPtr> LoadServerLibraries(string odaPath, Bitness bitness, params string[] libPaths)
        {
            var serverPath = Path.Combine(odaPath, "server", Enum.GetName(typeof(Bitness), bitness));
            var output = new List<IntPtr>();
            foreach(var path in libPaths)
            {
                var assembly = LoadLibraryEx(Path.Combine(serverPath, path), IntPtr.Zero, 8U);
                output.Add(assembly);
            }
            return output;
        }
        public static List<Assembly> LoadClientLibraries(string path, params string[] libPaths)
        {
            return libPaths.Select(libPath => Assembly.LoadFrom(Path.Combine(path, libPath))).ToList();
        }

        public static DirectoryInfo OdaFolder => OdaPath().Directory;
        
        public static FileInfo OdaPath()
        {
            string[] strArray = new string[2] { "oda", "odant" };
            foreach (string name1 in strArray)
            {
                try
                {
                    RegistryKey registryKey1 = Registry.ClassesRoot.OpenSubKey(name1);
                    if (registryKey1 != null)
                    {
                        string name2 = "shell\\open\\command";
                        RegistryKey registryKey2 = registryKey1.OpenSubKey(name2);
                        if (registryKey2 != null)
                        {
                            object obj = registryKey2.GetValue(string.Empty);
                            if (obj != null)
                            {
                                registryKey1.Close();
                                registryKey2.Close();
                                string str1 = obj.ToString();
                                string str2 = str1.Substring(str1.IndexOf("\"") + 1);
                                return new FileInfo(str2.Substring(0, str2.IndexOf("\"")));
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            return null;
        }
    }
}
