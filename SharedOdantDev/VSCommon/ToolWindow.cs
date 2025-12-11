using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms.Integration;
using Microsoft.VisualStudio.Shell;
using NativeMethods;
using Task = System.Threading.Tasks.Task;
using System.IO.Pipes;
using System.Threading;
using HorizontalAlignment = System.Windows.HorizontalAlignment;

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
    private static bool OutOfProcess => true;
    private static string OutOfProcessFolder => Path.Combine(ProcessEx.CurrentExecutingFolder().FullName, "app");
    private static string OutOfProcessPath => Path.Combine(OutOfProcessFolder, "OdantDevApp.exe");
    private Process ChildProcess { get; set; }
    private IntPtr ChildWindowHandle { get; set; }
    private WindowsFormsHost Host { get; }
    private IntPtr HostHandle { get; }

    private async Task RunDevAppAsync(bool restart = false)
    {
        var pipeName = $"odantdev-pipe-{Guid.NewGuid()}";
        using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var args = new CommandLineArgs { ProcessId = currentProcess.Id, PipeName = pipeName };
            var appPath = OutOfProcessPath;
            var process = ChildProcess = await StartProcessAsync(appPath, args).ConfigureAwait(true);

            using var ctsConnect = new CancellationTokenSource(60000);
            await pipeServer.WaitForConnectionAsync(ctsConnect.Token).ConfigureAwait(true);

            using var ctsRead = new CancellationTokenSource(30000);
            var handleBytes = new byte[IntPtr.Size];
            var readBytes = await pipeServer.ReadAsync(handleBytes, 0, handleBytes.Length, ctsRead.Token).ConfigureAwait(true);
            if (readBytes == 0)
            {
                await ShowErrorAsync("Can't read data from named pipe");
                return;
            }

            var processHandle = ChildWindowHandle = new IntPtr(BitConverter.ToInt64(handleBytes, 0));

            WinApi.SetWindow(
                processHandle,
                WindowLongFlags.GWL_STYLE, 
                new IntPtr((uint)(WindowStyles.WS_CHILD | WindowStyles.WS_BORDER | WindowStyles.WS_VISIBLE))
            );
            WinApi.SetParent(processHandle, HostHandle);
            WinApi.MoveWindow(processHandle, 0, 0, Host.Child.Width, Host.Child.Height, true);

            RestartIfFail(process);
            if (!restart)
            {
                Host.SizeChanged += (_, _) => HostWindowChanged();
                Host.DpiChanged += (_, _) => HostWindowChanged();
                Host.IsVisibleChanged += (_, _) => HostWindowChanged();
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex.ToString());
        }
    }

    private void RestartIfFail(Process process)
    {
        process.EnableRaisingEvents = true;
        process.Exited += Process_Exited;
    }

    private void Process_Exited(object sender, EventArgs e)
    {
        if (sender is not Process process)
            return;
        switch (process.ExitCode)
        {
            case (int)ExitCodes.Success:
            case (int)ExitCodes.Killed:
                break;
            case < 0:
            case (int)ExitCodes.Restart:
                _ = RunDevAppAsync(true);
                break;
            default:
                _ = ShowErrorAsync($"Unexpected exit code: {process.ExitCode}");
                break;
        }
    }

    private async Task ShowErrorAsync(string message)
    {
        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        var label = new System.Windows.Forms.Label
        {
            ForeColor = Color.IndianRed,
            Text = message
        };
        Host.Child = label;
    }

    private static WindowsFormsHost CreateHostWindow()
    {
        var imgPath = Path.Combine(ProcessEx.CurrentExecutingFolder().FullName, "Spinner.gif");
        var bitmap = new Bitmap(imgPath);
        var pb = new System.Windows.Forms.PictureBox
        {
            Image = bitmap,
            Dock = System.Windows.Forms.DockStyle.Fill,
            SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage
        };

        var host = new WindowsFormsHost
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,           
            Child = new System.Windows.Forms.Panel
            {
                Dock = System.Windows.Forms.DockStyle.Fill
            }
        };

        host.Child.Controls.Add(pb);
        return host;
    }

    private static async Task<Process> StartProcessAsync(string path, CommandLineArgs arguments)
    {
        return await Task.Run(() =>
        {
            var psi = new ProcessStartInfo(path, arguments.SerializeBinary())
            {
                WorkingDirectory = OutOfProcessFolder,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = false
            };

            //Для hot-reload дебаггера
#if DEBUG
            psi.EnvironmentVariables["COMPLUS_ForceENC"] = "1";
#endif
            var process = Process.Start(psi) ?? throw new Exception($"Can't start addin process {path}");
            return process;
        });
    }

    private void HostWindowChanged()
    {
        if (Host?.Child == null || ChildWindowHandle == IntPtr.Zero) return;
        Host.UpdateLayout();
        WinApi.MoveWindow(ChildWindowHandle, 0, 0, Host.Child.Width, Host.Child.Height, true);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindow"/> class.
    /// </summary>
    public ToolWindow()
    {
        Caption = "ODANT Dev";

        BitmapImageMoniker = Microsoft.VisualStudio.Imaging.KnownMonikers.AbstractCube;
        if (OutOfProcess)
        {
            base.Content = Host = CreateHostWindow();
            HostHandle = Host.Child.Handle;
        }
        else
        {
            OdantDevApp.VSCommon.EnvDTE.Instance = OdantDevPackage.EnvDte;
            base.Content = new ToolWindowControl();
        }
    }

    public override void OnToolWindowCreated()
    {
        base.OnToolWindowCreated();
        if (!OutOfProcess) return;
        _ = RunDevAppAsync();
    }
}