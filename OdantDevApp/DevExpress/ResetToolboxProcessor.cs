// Decompiled with JetBrains decompiler
// Type: DevExpress.ProjectUpgrade.Package.ResetToolboxProcessor
// Assembly: DevExpress.ProjectUpgrade.Package.Async.2022, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b88d1754d700e49a
// MVID: E043B518-C45C-4005-9918-F43EDCB8C9DE
// Assembly location: C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\Extensions\DevExpress\ProjectConverter\DevExpress.ProjectUpgrade.Package.Async.2022.dll

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace DevExpress.ProjectUpgrade.Package
{
  public static class ResetToolboxProcessor
  {
    private const string ToolboxAssembliesKeyLMFormat = "SOFTWARE\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstaller";
    private const string DXperienceKeyLM = "SOFTWARE\\DevExpress\\DXperience";
    private const string VSLocalDataRelativePath_Format = "Microsoft\\VisualStudio\\{0}";
    private const string KeyToolboxControlsInstallerCacheCU_Format = "Software\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstallerCache";
    private const string KeyToolboxControlsInstallerAssemblyFoldersExCacheCU_Format = "Software\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstaller_AssemblyFoldersExCache";

    public static void ResetToolbox(string vsVersion, string vsLocalDataPath)
    {
      ResetToolboxProcessor.DeleteTBDFiles(vsLocalDataPath);
      ResetToolboxProcessor.ClearToolboxRegistryCache(vsVersion);
    }

    private static string GetToolboxAssembliesKeyLM(string vsVersion)
    {
      return string.Format("SOFTWARE\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstaller", (object) vsVersion);
    }

    public static string[] GetInstalledDXPVersions()
    {
      RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\DevExpress\\DXperience");
      return registryKey == null ? new string[0] : registryKey.GetSubKeyNames();
    }

    public static List<string> GetToolboxInstalledCategories(
      string vsVersion,
      string dxpVersion)
    {
      List<string> stringList = new List<string>();
      RegistryKey registryKey1 = Registry.LocalMachine.OpenSubKey(ResetToolboxProcessor.GetToolboxAssembliesKeyLM(vsVersion));
      if (registryKey1 == null)
        return stringList;
      foreach (string subKeyName in registryKey1.GetSubKeyNames())
      {
        if (subKeyName.IndexOf("devexpress", StringComparison.InvariantCultureIgnoreCase) >= 0 && subKeyName.Contains(dxpVersion))
        {
          RegistryKey registryKey2 = registryKey1.OpenSubKey(string.Format("{0}\\ItemCategories", (object) subKeyName));
          if (registryKey2 != null)
          {
            foreach (string valueName in registryKey2.GetValueNames())
            {
              string str = (string) registryKey2.GetValue(valueName);
              if (!stringList.Contains(str))
                stringList.Add(str);
            }
          }
        }
      }
      return stringList;
    }

    private static string GetVSLocalDataRelativePath(string vsVersion)
    {
      return string.Format("Microsoft\\VisualStudio\\{0}", (object) vsVersion);
    }

    private static void DeleteTBDFiles(string vsLocalDataPath)
    {
      if (!Directory.Exists(vsLocalDataPath))
        return;
      foreach (string file in Directory.GetFiles(vsLocalDataPath, "*.tbd", SearchOption.TopDirectoryOnly))
      {
        try
        {
          File.Delete(file);
          ToolboxReseter.AddToLog("Deleted: " + file);
        }
        catch (Exception ex)
        {
          ToolboxReseter.AddToLog(ex.ToString());
        }
      }
    }

    private static string GetKeyToolboxControlsInstallerCacheCU(string vsVersion)
    {
      return string.Format("Software\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstallerCache", (object) vsVersion);
    }

    private static string GetKeyToolboxControlsInstallerAssemblyFoldersExCacheCU(string vsVersion)
    {
      return string.Format("Software\\Microsoft\\VisualStudio\\{0}\\ToolboxControlsInstaller_AssemblyFoldersExCache", (object) vsVersion);
    }

    private static void ClearToolboxRegistryCache(string vsVersion)
    {
      try
      {
        if (Registry.CurrentUser.OpenSubKey(ResetToolboxProcessor.GetKeyToolboxControlsInstallerCacheCU(vsVersion), true) != null)
        {
          Registry.CurrentUser.DeleteSubKeyTree(ResetToolboxProcessor.GetKeyToolboxControlsInstallerCacheCU(vsVersion));
          ToolboxReseter.AddToLog("Deleted: " + ResetToolboxProcessor.GetKeyToolboxControlsInstallerCacheCU(vsVersion));
        }
        if (Registry.CurrentUser.OpenSubKey(ResetToolboxProcessor.GetKeyToolboxControlsInstallerAssemblyFoldersExCacheCU(vsVersion), true) == null)
          return;
        Registry.CurrentUser.DeleteSubKeyTree(ResetToolboxProcessor.GetKeyToolboxControlsInstallerAssemblyFoldersExCacheCU(vsVersion));
        ToolboxReseter.AddToLog("Deleted: " + ResetToolboxProcessor.GetKeyToolboxControlsInstallerAssemblyFoldersExCacheCU(vsVersion));
      }
      catch (Exception ex)
      {
        ToolboxReseter.AddToLog(ex.ToString());
      }
    }
  }
}
