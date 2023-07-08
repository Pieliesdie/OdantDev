using System;
using System.Linq;

using OdantDev;

namespace OdantDevApp.Model;

public static class CommandLine
{
    private static CommandLineArgs _args;
    private static CommandLineArgs Args()
    {
        try
        {
            _args ??= Environment.GetCommandLineArgs().LastOrDefault()?.DeserializeBinary<CommandLineArgs>();
            return _args;
        }
        catch
        {
            return default;
        }
    }

    public static bool IsOutOfProcess => Args() != null;

    public static System.Diagnostics.Process TryGetParentProcess()
    {
        try
        {
            var args = Args();
            if (args == null) return null;
            var VSId = Args().ProcessId;
            var VSProcess = System.Diagnostics.Process.GetProcessById(VSId);
            return VSProcess;
        }
        catch
        {
            return null;
        }
    }
}
