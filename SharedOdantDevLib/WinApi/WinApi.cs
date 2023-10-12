using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace NativeMethods;

public static partial class WinApi
{
    [DllImport("ole32")]
    public static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);

    [DllImport("ole32")]
    public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32", SetLastError = true)]
    public static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32", EntryPoint = "GetWindowLongA", SetLastError = true)]
    public static extern WindowStyles GetWindowLong(IntPtr hwnd, WindowLongFlags nIndex);

    [DllImport("user32", EntryPoint = "SetWindowLongA", SetLastError = true)]
    public static extern long SetWindowLong(IntPtr hwnd, WindowLongFlags nIndex, WindowStyles dwNewLong);

    [DllImport("user32", SetLastError = true)]
    public static extern long SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

    [DllImport("user32", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

    [DllImport("user32")]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);

    [DllImport("user32", EntryPoint = "PostMessageA", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);

    [DllImport("user32")]
    public static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

    [DllImport("user32")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}