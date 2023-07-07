using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using OdantDev;

using OdantDevApp.Model;

using SharedOdantDevLib;

namespace OdantDevApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    void Application_Startup(object sender, StartupEventArgs e)
    {
        //#if DEBUG
        //            MessageBox.Show("Wait for debugger attach");
        //#endif
        Task.Run(StartCheckingForZombie);
    }

    void StartCheckingForZombie()
    {
        if (CommandLine.TryGetParentProcess() is Process parent)
        {
            _ = TaskEx.StartInfiniteTask(() => CheckForZombie(parent), TimeSpan.FromSeconds(4));
        }
    }

    void CheckForZombie(Process process)
    {
        if (process.HasExited)
        {
            Environment.Exit((int)ExitCodes.Killed);
        }
    }
}
