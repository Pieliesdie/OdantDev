using System;
using System.Runtime.InteropServices;

namespace OdantDev;

public static class WinApi
{
    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId", SetLastError = true,
       CharSet = CharSet.Unicode, ExactSpelling = true,
       CallingConvention = CallingConvention.StdCall)]
    internal static extern long GetWindowThreadProcessId(long hWnd, long lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern long SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongA", SetLastError = true)]
    internal static extern long GetWindowLong(IntPtr hwnd, int nIndex);

    [DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "SetWindowLongA", SetLastError = true)]
    internal static extern long SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern long SetWindowPos(IntPtr hwnd, IntPtr hWndInsertAfter, long x, long y, long cx, long cy, long wFlags);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool MoveWindow(IntPtr hwnd, int x, int y, int cx, int cy, bool repaint);

    [DllImport("user32.dll", EntryPoint = "PostMessageA", SetLastError = true)]
    internal static extern bool PostMessage(IntPtr hwnd, uint Msg, long wParam, long lParam);

    [DllImport("user32.dll")]
    internal static extern bool SetMenu(IntPtr hWnd, IntPtr hMenu);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    internal const int GWL_STYLE = (-16);
    internal const int WS_VISIBLE = 0x10000000;
    internal const int SW_HIDE = 0;
}