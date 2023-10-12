namespace NativeMethods;

[Flags]
public enum OleMessageFilterFlags : int
{
    SERVERCALL_ISHANDLED  = 0,
    SERVERCALL_RETRYLATER = 2,
    PENDINGMSG_WAITDEFPROCESS = 2,
}