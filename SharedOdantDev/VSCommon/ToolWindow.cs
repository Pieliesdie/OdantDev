using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;

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
    /// <summary>
    /// Initializes a new instance of the <see cref="ToolWindow"/> class.
    /// </summary>
    public ToolWindow() : base(null)
    {        
        this.Caption = "ODANT Dev";
        this.Content = new ToolWindow1Control(OdantDevPackage.Env_DTE);
    }

    protected override void Initialize()
    {
        base.Initialize();
        var test = this.GetIVsWindowPane();
    }
}