using System.Diagnostics;

namespace SharedOdantDevLib;

public static class ProcessEx
{
    private static readonly TimeSpan defaultDelayForSoftKill = TimeSpan.FromMilliseconds(3000);
    public static void TrySoftKill(this Process process, TimeSpan waitForHardKill = default(TimeSpan))
    {
        if (waitForHardKill == default)
            waitForHardKill = TimeSpan.FromSeconds(5);

        process.CloseMainWindow();
        process.WaitForExit(waitForHardKill.Milliseconds);
        if (!process.HasExited)
        {
            process.Kill();
        }
    }
}
