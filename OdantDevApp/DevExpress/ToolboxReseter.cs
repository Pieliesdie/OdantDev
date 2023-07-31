using System;
using System.Collections.Generic;
using System.Threading;

using DevExpress.Web.Projects.Menu.Package;

using EnvDTE;

using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell.Interop;

using Image = System.Drawing.Image;
using Window = EnvDTE.Window;

namespace DevExpress.ProjectUpgrade.Package;

internal class ToolboxReseter : IDisposable
{
    private ToolboxReseterSettings settings;
    private DTE dte;
    private IVsShell shellService;
    private string vsLocalDataPath;
    private WindowEvents m_WindowEvents;

    public ToolboxReseter(string vsLocalDataPath)
    {
        this.vsLocalDataPath = vsLocalDataPath;
        this.settings = ToolboxReseterSettings.Load();
    }

    public static void AddToLog(string str)
    {
    }

    public void DoFinish()
    {
        ToolboxReseter.AddToLog(nameof(DoFinish));
        try
        {
            if (!this.settings.IsAlreadyResetedToolbox || this.dte == null)
                return;
            ResetToolboxProcessor.ResetToolbox(this.dte.Version, this.vsLocalDataPath);
        }
        catch (Exception ex)
        {
            ToolboxReseter.AddToLog(ex.ToString());
        }
    }

    public void Start(DTE dte, IVsShell shellService)
    {
        try
        {
            this.dte = dte;
            this.shellService = shellService;
            if (dte.Version == "9.0")
                return;
            ToolboxReseter.AddToLog("OnVSStartupComplete: ToolboxReseter");
            this.CreateMenu();
            if (this.settings.IsAlreadyResetedToolbox)
            {
                this.settings.IsAlreadyResetedToolbox = false;
                this.settings.Save();
            }
            else
            {
                if (!this.settings.IsRequestResetToolbox)
                    return;
                try
                {
                    m_WindowEvents = dte.Events.get_WindowEvents();
                    m_WindowEvents.WindowActivated += m_WindowEvents_WindowActivated;
                }
                catch (Exception ex)
                {
                    ToolboxReseter.AddToLog(ex.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            ToolboxReseter.AddToLog(ex.ToString());
        }
    }

    private void CreateMenu()
    {
        return;

        ToolboxReseter.AddToLog("LoadMenu");
        Microsoft.VisualStudio.CommandBars.CommandBars commandBars = this.dte.CommandBars as Microsoft.VisualStudio.CommandBars.CommandBars;
        string reseterMenuItemName = "Repair toolbox";
        CommandBarButton button = MenuBarHelper.CreateButton(this.dte, ((_CommandBars)commandBars)[((object)"Toolbox")], reseterMenuItemName, true, null);
        ((_CommandBarButton)button).BeginGroup = true;
        // ISSUE: method pointer
        ((_CommandBarButtonEvents_Event)button).Click += ToolboxReseterButton_Click;
        // ISSUE: method pointer
        ((_CommandBarButtonEvents_Event)MenuBarHelper.CreateButton(this.dte, MenuBarHelper.FindDXMenuPopup(this.dte), reseterMenuItemName, true, (Image)null)).Click += ToolboxReseterButton_Click;
    }

    private void m_WindowEvents_WindowActivated(Window GotFocus, Window LostFocus)
    {
        try
        {
            if (LostFocus == null || LostFocus.ObjectKind != EnvDTE.Constants.vsWindowKindToolbox)
                return;
            // ISSUE: method pointer
            ((_dispWindowEvents_Event)this.m_WindowEvents).WindowActivated += m_WindowEvents_WindowActivated;
            string[] installedDxpVersions = new[] { "18.1" }; //ResetToolboxProcessor.GetInstalledDXPVersions();
            List<string> stringList = new List<string>((IEnumerable<string>)installedDxpVersions);
            foreach (string dxpVersion in installedDxpVersions)
            {
                List<string> installedCategories = ResetToolboxProcessor.GetToolboxInstalledCategories(this.dte.Version, dxpVersion);
                ToolBoxTabs toolBoxTabs = (LostFocus.Object as ToolBox).ToolBoxTabs;
                for (int index = 1; index <= toolBoxTabs.Count; ++index)
                {
                    ToolboxReseter.AddToLog(string.Format("Check tab: {0}", (object)toolBoxTabs.Item((object)index).Name));
                    if (installedCategories.Contains(toolBoxTabs.Item((object)index).Name))
                    {
                        stringList.Remove(dxpVersion);
                        break;
                    }
                }
            }
            if (stringList.Count <= 0)
                return;
            // var windowResetQuestion = new WindowResetQuestion(stringList.ToArray());
            bool flag = true;//windowResetQuestion.ShowDialog(this.dte.MainWindow).Value;
                             // if (!windowResetQuestion.IsShowMore)
                             // {
            this.settings.IsRequestResetToolbox = false;
            this.settings.Save();
            // }
            if (!flag)
                return;
            this.ResetToolboxQueue();
        }
        catch (Exception ex)
        {
            ToolboxReseter.AddToLog(ex.ToString());
        }
    }

    public void ResetToolboxQueue()
    {
        this.settings.IsRequestResetToolbox = true;
        this.settings.Save();
        this.ResetToolbox();
    }

    private void ToolboxReseterButton_Click(CommandBarButton Ctrl, ref bool CancelDefault)
    {
        try
        {
            ToolboxReseter.AddToLog("btn_Click");
            this.ResetToolboxQueue();
        }
        catch (Exception ex)
        {
            ToolboxReseter.AddToLog(ex.ToString());
        }
    }

    private void ResetToolbox()
    {
        new System.Threading.Thread((ThreadStart)(() =>
        {
            try
            {
                if (((IVsShell4)this.shellService).Restart(0U) != 0)
                    return;
                this.settings.IsAlreadyResetedToolbox = true;
                this.settings.Save();
            }
            catch (Exception ex)
            {
                ToolboxReseter.AddToLog(ex.ToString());
            }
        })).Start();
    }

    public void Dispose()
    {
        if (this.m_WindowEvents == null)
            return;

        this.m_WindowEvents.WindowActivated -= m_WindowEvents_WindowActivated;
        this.m_WindowEvents = (WindowEvents)null;
    }
}
