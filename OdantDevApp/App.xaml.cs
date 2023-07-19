using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using OdantDev;

using OdantDevApp.Model;

namespace OdantDevApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void Application_Startup(object sender, StartupEventArgs e)
    {
        _ = Task.Run(StartCheckingForZombie);
    }

    void StartCheckingForZombie()
    {
        if (CommandLine.TryGetParentProcess() is Process parent)
        {
            _ = TaskEx.StartInfiniteTask(() => CheckForZombie(parent), TimeSpan.FromSeconds(4));
        }
    }

    static void CheckForZombie(Process process)
    {
        if (process.HasExited)
        {
            Environment.Exit((int)ExitCodes.Killed);
        }
    }
}
