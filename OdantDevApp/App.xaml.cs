using System.Diagnostics;
using System.Runtime.InteropServices;
using OdantDevApp.Model;

namespace OdantDevApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);

    [DllImport("user32.dll")]
    private static extern IntPtr SetProcessDpiAwarenessContext(IntPtr dpiContext);

    public static void EnablePerMonitorV2()
    {
        // Вызывать как можно раньше (до создания форм/окон)
        try
        {
            SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
        }
        catch
        {
            // на старых ОС может не быть функции — игнорируем
        }
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        if (CommandLine.TryGetParentProcess() is { } parent)
        {
            parent.Exited += (_, _) => Environment.Exit((int)ExitCodes.Killed);
        }
    }
}