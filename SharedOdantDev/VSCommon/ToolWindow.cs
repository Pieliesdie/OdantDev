using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;

using Microsoft.VisualStudio.Shell;

using SharedOdantDevLib;

using Task = System.Threading.Tasks.Task;

namespace OdantDev;

/// <summary>
/// This class implements the tool window exposed by this package and hosts a user control.
/// </summary>
/// <remarks>
/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
/// usually implemented by the package implementer.
/// <para>
/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
/// implementation of the IVsUIElementPane interface.
/// </para>
/// </remarks>
[Guid("e477ca93-32f7-4a68-ab0d-7472ff3e7964")]
public class ToolWindow : ToolWindowPane
{
    private bool OutOfProcess => true;
    private string OutOfProcessFolder => Path.Combine(ProcessEx.CurrentExecutingFolder().FullName, "app");
    private string OutOfProcessPath => Path.Combine(OutOfProcessFolder, "OdantDevApp.exe");
    private Process ChildProcess { get; set; }
    private WindowsFormsHost Host { get; }
    private IntPtr HostHandle { get; }
    private async Task RunDevApp(bool restart = false)
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var args = new CommandLineArgs() { ProcessId = currentProcess.Id };
            var appPath = OutOfProcessPath;
            var process = ChildProcess = await StartProcessAsync(appPath, args);
            WinApi.SetParent(process.MainWindowHandle, HostHandle);
            WinApi.SetWindowLong(process.MainWindowHandle, WinApi.GWL_STYLE, WinApi.WS_VISIBLE);
            //Remove border and whatnot
            WinApi.MoveWindow(process.MainWindowHandle, 0, 0, (int)Host.ActualWidth, (int)Host.ActualHeight, true);

            RestartIfFail(process);
            if (!restart)
            {
                SubscribeToSizeChanging(Host);
            }
        }
        catch (Exception ex)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Host.Child = new Label() { Text = ex.ToString() };
        }
    }

    private void RestartIfFail(Process process)
    {
        process.EnableRaisingEvents = true;
        process.Exited += Process_Exited;
    }

    private async void Process_Exited(object sender, EventArgs e)
    {
        if (sender is not Process process)
            return;
        switch (process.ExitCode)
        {
            case (int)ExitCodes.Success:
                break;
            case < 0:
            case (int)ExitCodes.Restart:
                _ = RunDevApp(true);
                break;
            default:
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                Host.Child = new System.Windows.Forms.Label() { Text = $"Unexpected exit code: {process.ExitCode}" };
                break;
        }
    }

    private WindowsFormsHost CreateHost()
    {
        var imgPath = Path.Combine(ProcessEx.CurrentExecutingFolder().FullName, "Spinner.gif");
        var bitmap = new Bitmap(imgPath);
        var pb = new PictureBox()
        {
            Image = bitmap,
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.AutoSize,
            Anchor = AnchorStyles.None
        };
        WindowsFormsHost host = new()
        {
            Child = new Panel()
            {
                Dock = DockStyle.Fill
            }
        };
        host.Child.Controls.Add(pb);
        return host;
    }

    private async Task<Process> StartProcessAsync(string path, CommandLineArgs arguments)
    {
        return await Task.Run(() =>
        {
            var psi = new ProcessStartInfo(path, arguments.SerializeBinary())
            {
                WorkingDirectory = OutOfProcessFolder,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };
            var process = Process.Start(psi);

            try
            {
                while (process.MainWindowHandle == IntPtr.Zero)
                {
                    process.Refresh();
                }
            }
            catch
            {
                process.Close();
                throw;
            }
            WinApi.ShowWindow(process.MainWindowHandle, WinApi.SW_HIDE);
            return process;
        });
    }

    private void SubscribeToSizeChanging(WindowsFormsHost host) => host.SizeChanged += Host_SizeChanged;

    private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ChildProcess is null || ChildProcess.MainWindowHandle == IntPtr.Zero) return;
        // Move the window to overlay it on this window
        WinApi.MoveWindow(ChildProcess.MainWindowHandle, 0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height, true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindow"/> class.
    /// </summary>
    public ToolWindow() : base()
    {
        this.Caption = "ODANT Dev";
        if (OutOfProcess)
        {
            this.Content = Host = CreateHost();
            HostHandle = Host.Child.Handle;
        }
        else
        {
            this.Content = new ToolWindow1Control(OdantDevPackage.Env_DTE);
        }
    }

    public override void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();
        if (!OutOfProcess) return;
        _ = RunDevApp();
    }
}