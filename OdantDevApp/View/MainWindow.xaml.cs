using System.IO.Pipes;
using System.Windows.Interop;
using OdantDevApp.Model;

namespace OdantDevApp;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        if (CommandLine.IsOutOfProcess)
        {
            ShowInTaskbar = false;
            Loaded += MainWindow_Loaded;
        }
    }

    private void MainWindow_Loaded(object sender, EventArgs e)
    {
        try
        {
            var args = CommandLine.Args;
            if (string.IsNullOrEmpty(args?.PipeName))
            {
                return;
            }
            var windowHandle = new WindowInteropHelper(this).Handle;
            using var pipeClient = new NamedPipeClientStream(".", args.PipeName, PipeDirection.Out);
            pipeClient.Connect(5000);
            var handleBytes = BitConverter.GetBytes(windowHandle.ToInt64());
            pipeClient.Write(handleBytes, 0, handleBytes.Length);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to connect to Visual Studio: {ex.Message}");
            Application.Current.Shutdown();
        }
    }
}
