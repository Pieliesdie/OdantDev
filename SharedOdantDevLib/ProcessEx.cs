using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
