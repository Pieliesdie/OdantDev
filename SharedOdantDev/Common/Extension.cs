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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OdantDev;

public static class Extension
{
    public static Bitness Platform => IntPtr.Size == 4 ? Bitness.x86 : Bitness.x64;

    public static DirectoryInfo VSIXPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

    public static string SubstringBefore(this string str, string search)
    {
        var index = str.IndexOf(search);
        if (index > 0)
        {
            return str.Substring(0, index);
        }
        return str;
    }
    public static string SubstringAfter(this string str, string search, bool takeLast = false)
    {
        var index = (takeLast? str.LastIndexOf(search) : str.IndexOf(search));
        if (index >= 0)
        {
            return str.Substring(index + search.Length);
        }
        return str;
    }
    public static string Or(this string text, string alternative)
    {
        return string.IsNullOrWhiteSpace(text) ? alternative : text;
    }
    public static bool Remove<T>(this IList<T> list, Predicate<T> predicate)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<T> Use<T>(this IEnumerable<T> items) where T : IDisposable
    {
        foreach (var item in items)
            yield return item;
        foreach (var item in items)
        {
            if (item != null)
            {
                item.Dispose();
            }
        }
    }
    public static bool Not(this bool boolean) => !boolean;
    public static bool CopyToDir(this FileSystemInfo fileSystemInfo, DirectoryInfo destinationDir)
    {
        if (fileSystemInfo is DirectoryInfo directoryInfo)
        {
            CopyDirectory(directoryInfo, Directory.CreateDirectory(Path.Combine(destinationDir.FullName, directoryInfo.Name)));
            return true;
        }
        else if (fileSystemInfo is FileInfo fileInfo)
        {
            return fileInfo.CopyTo(Path.Combine(destinationDir.FullName, fileInfo.Name), true) != null;
        }
        return false;
    }

    public static void CopyDirectory(DirectoryInfo source, DirectoryInfo target)
    {
        Directory.CreateDirectory(target.FullName);

        // Copy each file into the new directory.
        foreach (FileInfo fi in source.GetFiles())
        {
            fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
        }

        // Copy each subdirectory using recursion.
        foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
        {
            DirectoryInfo nextTargetSubDir =
                target.CreateSubdirectory(diSourceSubDir.Name);
            CopyDirectory(diSourceSubDir, nextTargetSubDir);
        }
    }
    public static bool TryDeleteDirectory(this DirectoryInfo baseDir, int maxRetries = 10, int millisecondsDelay = 30)
    {
        if (baseDir == null)
            throw new ArgumentNullException(nameof(baseDir));
        if (maxRetries < 1)
            throw new ArgumentOutOfRangeException(nameof(maxRetries));
        if (millisecondsDelay < 1)
            throw new ArgumentOutOfRangeException(nameof(millisecondsDelay));

        for (int i = 0; i < maxRetries; ++i)
        {
            try
            {
                if (baseDir.Exists)
                {
                    baseDir.Delete(true);
                }
                return true;
            }
            catch (IOException)
            {
                Thread.Sleep(millisecondsDelay);
            }
            catch (UnauthorizedAccessException)
            {
                Thread.Sleep(millisecondsDelay);
            }
        }

        return false;
    }

    public static BitmapImage ConvertToBitmapImage(this Bitmap src)
    {
        if (src == null) return null;
        using MemoryStream ms = new MemoryStream();
        src.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        ms.Seek(0, SeekOrigin.Begin);
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
    public static string ToBase64String(this BitmapImage src)
    {
        if (src == null) return null;
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(src);
        encoder.Frames.Add(frame);
        using (var stream = new MemoryStream())
        {
            encoder.Save(stream);
            return Convert.ToBase64String(stream.ToArray());
        }
    }
    public static BitmapImage FromBase64String(this string src)
    {
        if (src == null) return null;
        byte[] binaryData = Convert.FromBase64String(src);
        using MemoryStream ms = new MemoryStream(binaryData);
        ms.Seek(0, SeekOrigin.Begin);
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = ms;
        image.EndInit();
        image.Freeze();
        return image;
    }
    public static List<IntPtr> LoadServerLibraries(string odaPath, Bitness bitness, params string[] libPaths)
    {
        var serverPath = Path.Combine(odaPath, "server", Enum.GetName(typeof(Bitness), bitness));
        var output = new List<IntPtr>();
        foreach (var path in libPaths)
        {
            var assembly = ServerApi.LoadLibraryEx(Path.Combine(serverPath, path), IntPtr.Zero, 8U);
            output.Add(assembly);
        }
        return output;
    }
    public static List<Assembly> LoadClientLibraries(string path, params string[] libPaths)
    {
        return libPaths.Select(libPath => Assembly.LoadFrom(Path.Combine(path, libPath))).ToList();
    }

    public static DirectoryInfo LastOdaFolder = LastOdaPath().Directory;

    public static FileInfo LastOdaPath()
    {
        string[] strArray = new string[] { "oda", "odant", "Applications\\ODA.exe" };
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
