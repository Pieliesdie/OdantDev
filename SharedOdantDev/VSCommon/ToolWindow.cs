using Microsoft.VisualStudio.Shell;

using SharedOdantDevLib;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

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
    private bool OutOfProcess = true;
    private Process ChildProcess { get; set; }
    private WindowsFormsHost Host { get; }
    private IntPtr HostHandle { get; }
    private async Task RunDevApp(bool restart = false)
    {
        var currentProcess = Process.GetCurrentProcess();
        var args = new CommandLineArgs() { ProcessId = currentProcess.Id };
        var process = ChildProcess = await StartProcessAsync(@"OdantDevApp.exe", args);
        WinApi.SetParent(process.MainWindowHandle, HostHandle);
        WinApi.SetWindowLong(process.MainWindowHandle, WinApi.GWL_STYLE, WinApi.WS_VISIBLE);
        //Remove border and whatnot
        WinApi.MoveWindow(process.MainWindowHandle, 0, 0, (int)Host.ActualWidth, (int)Host.ActualHeight, true);

        KillAfterExit(process);
        RestartIfFail(process);
        if (!restart)
        {
            SubscribeToSizeChanging(Host);
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
        if (process.ExitCode == (int)ExitCodes.Restart)
        {
            await RunDevApp();
        }
    }

    private WindowsFormsHost CreateHost()
    {
        WindowsFormsHost host = new()
        {
            Child = new System.Windows.Forms.Panel() { Dock = System.Windows.Forms.DockStyle.Fill, }
        };
        return host;
    }

    private async void KillAfterExit(Process process)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        Action kill = () =>
        {
            if (process.HasExited) return;
            try
            {
                process?.Kill(); process?.WaitForExit();
            }
            catch { }
        };
        Application.Current.Exit += (s, e) => { kill(); };
        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            kill();
        };
        AppDomain.CurrentDomain.DomainUnload += (s, e) =>
        {
            kill();
        };
    }

    private async Task<Process> StartProcessAsync(string path, CommandLineArgs arguments)
    {
        return await Task.Run(() =>
        {
            ProcessStartInfo psi = new ProcessStartInfo(path, arguments.SerializeBinary())
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden, 
                UseShellExecute = true
            };
            var process = Process.Start(psi);
            while (process.MainWindowHandle == IntPtr.Zero)
            {
                process.Refresh();
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
            this.Content = new ToolWindow1Control() { DTE2 = OdantDevPackage.Env_DTE };
        }
    }

    public override async void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();
        if (!OutOfProcess) return;
        await RunDevApp();
    }
}