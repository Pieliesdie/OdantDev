using EnvDTE;

using EnvDTE80;

using OdantDev;

using OdantDevApp.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OdantDevApp.VSCommon;

public static class ExternalEnvDTE
{
    public static DTE2 Instance { get { Init(); return instance; } set => instance = value; }

    private static DTE2 instance;

    private static void Init()
    {
        if (instance != null) { return; }
        if (instance == null && CommandLine.TryGetParentProcess() is System.Diagnostics.Process parentProcess)
        {
            instance = GetDTE(parentProcess.Id, 5) as DTE2;
        }
    }

    #region EnvDTE from running process
    /// 
    /// Gets the DTE object from any devenv process.
    /// 
    /// 
    /// After starting devenv.exe, the DTE object is not ready. We need to try repeatedly and fail after the
    /// timeout.
    /// 
    /// 
    /// Timeout in seconds.
    /// 
    /// Retrieved DTE object or  if not found.
    /// 
    private static DTE GetDTE(int processId, int timeout)
    {
        DTE res = null;
        DateTime startTime = DateTime.Now;

        while (res == null && DateTime.Now.Subtract(startTime).Seconds < timeout)
        {
            System.Threading.Thread.Sleep(1000);
            res = GetDTE(processId);
        }

        return res;
    }

    /// 
    /// Gets the DTE object from any devenv process.
    /// 
    /// 
    /// 
    /// Retrieved DTE object or  if not found.
    /// 
    private static DTE GetDTE(int processId)
    {
        object runningObject = null;

        IBindCtx bindCtx = null;
        IRunningObjectTable rot = null;
        IEnumMoniker enumMonikers = null;

        try
        {
            Marshal.ThrowExceptionForHR(NativeMethods.CreateBindCtx(reserved: 0, ppbc: out bindCtx));
            bindCtx.GetRunningObjectTable(out rot);
            rot.EnumRunning(out enumMonikers);

            IMoniker[] moniker = new IMoniker[1];
            IntPtr numberFetched = IntPtr.Zero;
            while (enumMonikers.Next(1, moniker, numberFetched) == 0)
            {
                IMoniker runningObjectMoniker = moniker[0];

                string name = null;

                try
                {
                    if (runningObjectMoniker != null)
                    {
                        runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // Do nothing, there is something in the ROT that we do not have access to.
                }

                Regex monikerRegex = new Regex(@"!VisualStudio.DTE\.\d+\.\d+\:" + processId, RegexOptions.IgnoreCase);
                if (!string.IsNullOrEmpty(name) && monikerRegex.IsMatch(name))
                {
                    Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                    break;
                }
            }
        }
        finally
        {
            if (enumMonikers != null)
            {
                Marshal.ReleaseComObject(enumMonikers);
            }

            if (rot != null)
            {
                Marshal.ReleaseComObject(rot);
            }

            if (bindCtx != null)
            {
                Marshal.ReleaseComObject(bindCtx);
            }
        }

        return runningObject as DTE;
    }
    #endregion
}
