using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;

using EnvDTE;

using EnvDTE80;

using OdantDevApp.Model;

namespace OdantDevApp.VSCommon;

public static class EnvDTE
{
    public static DTE2? Instance { get { Init(); return _instance; } set => _instance = value; }

    private static DTE2? _instance;

    private static void Init()
    {
        if (_instance != null) { return; }
        if (_instance == null && CommandLine.TryGetParentProcess() is { } parentProcess)
        {
            _instance = GetDTE(parentProcess.Id, 5);
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
    private static DTE2? GetDTE(int processId, int timeout)
    {
        DTE2 res = null;
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
    private static DTE2? GetDTE(int processId)
    {
        object runningObject = null;

        IBindCtx bindCtx = null;
        IRunningObjectTable rot = null;
        IEnumMoniker enumMonikers = null;

        try
        {
            Marshal.ThrowExceptionForHR(global::NativeMethods.WinApi.CreateBindCtx(reserved: 0, ppbc: out bindCtx));
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
                    runningObjectMoniker?.GetDisplayName(bindCtx, null, out name);
                }
                catch (UnauthorizedAccessException)
                {
                    // Do nothing, there is something in the ROT that we do not have access to.
                }

                Regex monikerRegex = new Regex(@"!VisualStudio.DTE\.\d+\.\d+\:" + processId, RegexOptions.IgnoreCase);
                Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out var runningObject1));
                if (!string.IsNullOrEmpty(name) && monikerRegex.IsMatch(name))
                {
                    Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                    //break;
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

        return runningObject as DTE2;
    }
    #endregion
}
