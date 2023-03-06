﻿using DevExpress.ProjectUpgrade.Package;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace OdantDev;

/// <summary>
/// This is the class that implements the package exposed by this assembly.
/// </summary>
/// <remarks>
/// <para>
/// The minimum requirement for a class to be considered a valid package for Visual Studio
/// is to implement the IVsPackage interface and register itself with the shell.
/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
/// to do it: it derives from the Package class that provides the implementation of the
/// IVsPackage interface and uses the registration attributes defined in the framework to
/// register itself and its components with the shell. These attributes tell the pkgdef creation
/// utility what data to put into .pkgdef file.
/// </para>
/// <para>
/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
/// </para>
/// </remarks>
[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[Guid(OdantDevPackage.PackageGuidString)]
[ProvideMenuResource("Menus.ctmenu", 1)]
[ProvideToolWindow(typeof(ToolWindow))]
public sealed class OdantDevPackage : AsyncPackage
{
    /// <summary>
    /// OdantDevPackage GUID string.
    /// </summary>
    public const string PackageGuidString = "eb973a1d-1dcf-454a-9868-2334c194385f";

    #region Package Members

    /// <summary>
    /// Initialization of the package; this method is called right after the package is sited, so this is the place
    /// where you can put all the initialization code that rely on services provided by VisualStudio.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
    /// <param name="progress">A provider for progress updates.</param>
    /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
    protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
    {
        await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        Env_DTE = await this.GetServiceAsync<DTE,DTE2>();

        try
        {
            IVsShell shellService = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
            ToolboxReseter = new ToolboxReseter(UserLocalDataPath);
            ToolboxReseter.Start(Env_DTE as DTE, shellService);
        }
        catch (Exception e)
        {
        }

        await ToolWindowCommand.InitializeAsync(this);
    }

    protected override void Dispose(bool disposing)
    {
        if (ToolboxReseter != null)
        {
            ToolboxReseter.DoFinish();
            ToolboxReseter.Dispose();
        }
        
        base.Dispose(disposing);
    }

    public static DTE2 Env_DTE { get; set; }

    internal static ToolboxReseter ToolboxReseter { get; set; }

    public static IServiceProvider ServiceProvider { get; set; }
    #endregion
}
