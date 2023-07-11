using System.Diagnostics;
using System.Reflection;

namespace SharedOdantDevLib;

public static class ProcessEx
{
    private static readonly TimeSpan defaultDelayForSoftKill = TimeSpan.FromMilliseconds(5000);
    public static void TrySoftKill(this Process process, TimeSpan waitForHardKill = default)
    {
        if (waitForHardKill == default)
            waitForHardKill = defaultDelayForSoftKill;

        process.CloseMainWindow();
        process.WaitForExit(waitForHardKill.Milliseconds);
        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    public static DirectoryInfo CurrentExecutingFolder()
    {
        return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;
    }
}
