namespace OdantDev;

public static class DirectoryEx
{
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
}
