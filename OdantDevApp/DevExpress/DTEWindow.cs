// Decompiled with JetBrains decompiler
// Type: DevExpress.ProjectUpgrade.Package.DTEWindow
// Assembly: DevExpress.ProjectUpgrade.Package.Async.2022, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a
// MVID: E043B518-C45C-4005-9918-F43EDCB8C9DE
// Assembly location: C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\DevExpress\ProjectConverter\DevExpress.ProjectUpgrade.Package.Async.2022.dll

using System;
using System.Windows.Interop;

namespace DevExpress.ProjectUpgrade.Package
{
    public class DTEWindow : System.Windows.Window
    {
        private System.Windows.Window windowImplementation;

        public DTEWindow()
        {
            this.ShowInTaskbar = false;
        }

        public bool? ShowDialog(EnvDTE.Window owner)
        {
            try
            {
                if (owner.DTE.Version == "9.0")
                    new WindowInteropHelper((System.Windows.Window)this).Owner = HelperMain.GetHWND(owner.HWnd);
                else
                    this.Owner = (System.Windows.Window)HwndSource.FromHwnd(HelperMain.GetHWND(owner.HWnd)).RootVisual;
            }
            catch (Exception ex)
            {
                ToolboxReseter.AddToLog(ex.ToString());
            }
            return this.ShowDialog();
        }
    }
}
