// Decompiled with JetBrains decompiler
// Type: DevExpress.ProjectUpgrade.Package.HelperMain
// Assembly: DevExpress.ProjectUpgrade.Package.Async.2022, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a
// MVID: E043B518-C45C-4005-9918-F43EDCB8C9DE
// Assembly location: C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\DevExpress\ProjectConverter\DevExpress.ProjectUpgrade.Package.Async.2022.dll

using EnvDTE;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Process = System.Diagnostics.Process;

namespace DevExpress.ProjectUpgrade.Package
{
  public static class HelperMain
  {
    public static string GetRegistryKeyRelative(string key)
    {
      return key.Replace(string.Format("{0}\\", (object) Registry.LocalMachine.Name), string.Empty).Replace(string.Format("{0}\\", (object) Registry.CurrentUser.Name), string.Empty);
    }

    public static List<Project> GetProjects(Solution solution)
    {
      return HelperMain.CollectProjects(((IEnumerable) ((_Solution) solution).Projects).OfType<Project>().ToList<Project>());
    }

    private static List<Project> CollectProjects(List<Project> rootProjects)
    {
      List<Project> projectList = new List<Project>();
      using (List<Project>.Enumerator enumerator = rootProjects.GetEnumerator())
      {
        while (enumerator.MoveNext())
        {
          Project current = enumerator.Current;
          try
          {
            if (current.ProjectItems != null)
            {
              if (current.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
              {
                List<Project> list = ((IEnumerable) current.ProjectItems).OfType<ProjectItem>().Select<ProjectItem, Project>((Func<ProjectItem, Project>) (pi => pi.SubProject)).ToList<Project>();
                projectList.AddRange((IEnumerable<Project>) HelperMain.CollectProjects(list));
              }
              else
                projectList.Add(current);
            }
          }
          catch
          {
          }
        }
      }
      return projectList;
    }

    public static IntPtr GetHWND(int value)
    {
      return new IntPtr(value);
    }

    public static IntPtr GetHWND(IntPtr value)
    {
      return value;
    }

    public static string GetStringShortVersion(int version)
    {
      string str1 = version.ToString();
      if (str1.Length < 3)
        return str1;
      string str2 = string.Format("v{0}", (object) str1.Insert(2, "."));
      return str1.Length == 3 ? str2 : str2.Insert(5, ".");
    }

    public static HelperMain.ProjectType GetProjectType(Project project)
    {
      string empty = string.Empty;
      string str;
      try
      {
        str = File.ReadAllText(project.FullName);
      }
      catch (Exception ex)
      {
        ToolboxReseter.AddToLog(ex.ToString());
        return HelperMain.ProjectType.Win;
      }
      if (str.IndexOf("{349C5851-65DF-11DA-9384-00065B846F21}", StringComparison.InvariantCultureIgnoreCase) > -1)
        return HelperMain.ProjectType.Web;
      if (str.IndexOf("{A1591282-1198-4647-A2B1-27E5FF5F6F3B}", StringComparison.InvariantCultureIgnoreCase) > -1)
        return HelperMain.ProjectType.SL;
      return str.IndexOf("{60DC8134-EBA5-43B8-BCC9-BB4BC16C2548}", StringComparison.InvariantCultureIgnoreCase) > -1 ? HelperMain.ProjectType.WPF : HelperMain.ProjectType.Win;
    }

    public static int GetVSCount(string vsVersion)
    {
      string studioPath = HelperMain.GetStudioPath(vsVersion);
      int num = 0;
      foreach (System.Diagnostics.Process process in Process.GetProcessesByName("devenv"))
      {
        string str;
        try
        {
          str = process.MainModule.FileName;
        }
        catch (Exception ex)
        {
          str = string.Empty;
        }
        if (str.IndexOf(studioPath, StringComparison.InvariantCultureIgnoreCase) != -1)
          ++num;
      }
      return num;
    }

    private static string GetStudioPath(string vsVersion)
    {
      string name = string.Format("SOFTWARE\\Microsoft\\VisualStudio\\{0}", (object) vsVersion);
      RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name);
      if (registryKey == null)
        return string.Empty;
      string empty = string.Empty;
      string path1 = (string) registryKey.GetValue("InstallDir");
      if (string.IsNullOrEmpty(path1))
        return string.Empty;
      string path = Path.Combine(path1, "devenv.exe");
      return !File.Exists(path) ? string.Empty : path;
    }

    public static int GetIntMajorVersion(string strVersion)
    {
      int result;
      return string.IsNullOrEmpty(strVersion) || !strVersion.Contains(".") || !int.TryParse(strVersion.Replace(".", string.Empty).Replace("v", string.Empty), out result) ? 0 : result;
    }

    public enum ProjectType
    {
      Win,
      Web,
      SL,
      WPF,
    }
  }
}
