// Decompiled with JetBrains decompiler
// Type: DevExpress.Web.Projects.Menu.Package.MenuBarHelper
// Assembly: DevExpress.ProjectUpgrade.Package.Async.2022, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a
// MVID: E043B518-C45C-4005-9918-F43EDCB8C9DE
// Assembly location: C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\DevExpress\ProjectConverter\DevExpress.ProjectUpgrade.Package.Async.2022.dll

using EnvDTE;
using Microsoft.VisualStudio.CommandBars;
using stdole;
using System;
using System.Collections;
using System.Drawing;
using System.Windows.Controls;

using Image = System.Drawing.Image;

namespace DevExpress.Web.Projects.Menu.Package
{
  public static class MenuBarHelper
  {
    private const string dxMenuName = "DevE&xpress";
    private const string dxSubMenuName = "E&xtensions";

    public static CommandBarPopup CreatePopup(DTE dte, string title)
    {
      CommandBar dxMenuPopup = MenuBarHelper.FindDXMenuPopup(dte);
      if (dxMenuPopup == null)
        return (CommandBarPopup) null;
      CommandBarPopup commandBarPopup = dxMenuPopup.Controls.Add((object) (MsoControlType) 10, Type.Missing, Type.Missing, Type.Missing, Type.Missing) as CommandBarPopup;
      commandBarPopup.Visible = (true);
      commandBarPopup.Enabled = (true);
      commandBarPopup.Caption = (title);
      commandBarPopup.TooltipText = (title);
      commandBarPopup.CommandBar.Name = (title);
      return commandBarPopup;
    }

    public static CommandBar FindDXMenuPopup(DTE dte)
    {
      CommandBar parentCommandBar = ((_CommandBars) (dte.CommandBars as Microsoft.VisualStudio.CommandBars.CommandBars))[((object) "MenuBar")];
      CommandBar commandBarPopup1 = MenuBarHelper.GetCommandBarPopup(parentCommandBar, "DevE&xpress");
      if (commandBarPopup1 != null)
        return commandBarPopup1;
      CommandBar commandBarPopup2 = MenuBarHelper.GetCommandBarPopup(parentCommandBar, "E&xtensions");
      return commandBarPopup2 == null ? (CommandBar) null : MenuBarHelper.GetCommandBarPopup(commandBarPopup2, "DevE&xpress") ?? commandBarPopup2;
    }

    public static CommandBarButton CreateButton(
      DTE dte,
      CommandBarPopup popup,
      string title,
      bool visible,
      Image image)
    {
      CommandBarButton commandBarButton = popup.CommandBar.Controls.Add((object) (MsoControlType) 1, Type.Missing, Type.Missing, Type.Missing, Type.Missing) as CommandBarButton;
      ((_CommandBarButton) commandBarButton).Visible = (visible);
      ((_CommandBarButton) commandBarButton).Enabled = (visible);
      ((_CommandBarButton) commandBarButton).Caption = title;
      ((_CommandBarButton) commandBarButton).TooltipText = (title);
      //if (image != null)
      //  ((_CommandBarButton) commandBarButton).Picture = (StdPicture) ImageToPictureDisp(image);
      return commandBarButton;
    }

    public static CommandBarButton CreateButton(
      DTE dte,
      CommandBar bar,
      string title,
      bool visible,
      Image image)
    {
      CommandBarButton commandBarButton = bar.Controls.Add((object) (MsoControlType) 1, Type.Missing, Type.Missing, Type.Missing, Type.Missing) as CommandBarButton;
      ((_CommandBarButton) commandBarButton).Visible = (visible);
      ((_CommandBarButton) commandBarButton).Enabled = (visible);
      ((_CommandBarButton) commandBarButton).Caption = (title);
      ((_CommandBarButton) commandBarButton).TooltipText = (title);
      ((_CommandBarButton) commandBarButton).Style = ((MsoButtonStyle) 3);
      //if (image != null)
      //  ((_CommandBarButton) commandBarButton).Picture = ImageToPictureDisp(image) as StdPicture;
      return commandBarButton;
    }

    public static CommandBar GetCommandBarPopup(
      CommandBar parentCommandBar,
      string commandBarPopupName)
    {
      IEnumerator enumerator = parentCommandBar.Controls.GetEnumerator();
      try
      {
        while (enumerator.MoveNext())
        {
          CommandBarControl current = (CommandBarControl) enumerator.Current;
          if (current.Type == MsoControlType.msoControlPopup)
          {
            CommandBarPopup commandBarPopup = (CommandBarPopup) current;
            if (commandBarPopup.CommandBar.Name == commandBarPopupName)
              return commandBarPopup.CommandBar;
          }
        }
      }
      finally
      {
        if (enumerator is IDisposable disposable)
          disposable.Dispose();
      }
      return (CommandBar) null;
    }
  }
}
