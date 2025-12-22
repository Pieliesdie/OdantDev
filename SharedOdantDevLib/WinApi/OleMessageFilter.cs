using NativeMethods;
using OdantDev;

namespace SharedOdantDevLib.WinApi;
public class OleMessageFilter : IOleMessageFilter
{
    public static IDisposable MessageFilterRegister()
    {
        Register();
        return Disposable.Create(Revoke);
    }
    //
    // Class containing the IOleMessageFilter
    // thread error-handling functions.

    public static void Register()
    {
        IOleMessageFilter newFilter = new OleMessageFilter();
        IOleMessageFilter oldFilter = null;
        NativeMethods.WinApi.CoRegisterMessageFilter(newFilter, out oldFilter);
    }

    // Done with the filter, close it.
    public static void Revoke()
    {
        IOleMessageFilter oldFilter = null;
        NativeMethods.WinApi.CoRegisterMessageFilter(null, out oldFilter);
    }

    private OleMessageFilter() { }

    // IOleMessageFilter functions.
    // Handle incoming thread requests.
    OleMessageFilterFlags IOleMessageFilter.HandleInComingCall(int dwCallType, IntPtr hTaskCaller, int dwTickCount, IntPtr lpInterfaceInfo)
    {
        return OleMessageFilterFlags.SERVERCALL_ISHANDLED;
    }

    // Thread call was rejected, so try again.
    int IOleMessageFilter.RetryRejectedCall(IntPtr hTaskCallee, int dwTickCount, OleMessageFilterFlags dwRejectType)
    {
        if (dwRejectType == OleMessageFilterFlags.SERVERCALL_RETRYLATER && dwTickCount < 10000)
        {
            // Retry the thread call after 250ms
            return 250;
        }
        // Too busy; cancel call.
        return -1;
    }

    OleMessageFilterFlags IOleMessageFilter.MessagePending(System.IntPtr hTaskCallee, int dwTickCount, int dwPendingType)
    {
        return OleMessageFilterFlags.PENDINGMSG_WAITDEFPROCESS;
    }
}
