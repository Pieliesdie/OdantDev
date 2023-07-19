using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace OdantDev;

public static class WinApi
{
    [DllImport("ole32.dll")]
    public static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true,
       CharSet = CharSet.Unicode, ExactSpelling = true,
       CallingConvention = CallingConvention.StdCall)]
    public static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
    public static extern long GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetWindowLongA", SetLastError = true)]
    public static extern long SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern long SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

    [DllImport("user32.dll")]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, int flags);

    [DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);

    [DllImport("user32.dll")]
    public static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public const int GWL_STYLE = (-16);
    public const int WS_VISIBLE = 0x10000000;
    public const int SW_HIDE = 0;
}