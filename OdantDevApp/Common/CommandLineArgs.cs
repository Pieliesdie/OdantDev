using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OdantDev;

using OdantDevApp.Common;

namespace OdantDevApp.Model;

public static class CommandLine
{
    private static CommandLineArgs _args;
    public static CommandLineArgs Args()
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

    public static System.Diagnostics.Process TryGetParentProcess()
    {
        try
        {
            var args = Args();
            if(args == null) return null;
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
