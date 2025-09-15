using System;
using System.Linq;

using OdantDev;

namespace OdantDevApp.Model;

public static class CommandLine
{
    public static CommandLineArgs? Args
    {
        get
        {
            try
            {
                field ??= Environment.GetCommandLineArgs().LastOrDefault()?.DeserializeBinary<CommandLineArgs>();
                return field;
            }
            catch
            {
                return null;
            }
        }
    }

    public static bool IsOutOfProcess => Args != null;

    public static System.Diagnostics.Process? TryGetParentProcess()
    {
        try
        {
            if (Args == null)
            {
                return null;
            }
            var vsId = Args.ProcessId;
            var vsProcess = System.Diagnostics.Process.GetProcessById(vsId);
            return vsProcess;
        }
        catch
        {
            return null;
        }
    }
}
