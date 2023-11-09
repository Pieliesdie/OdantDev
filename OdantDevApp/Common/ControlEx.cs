using System;
using System.Windows;
using System.Windows.Controls;

using MaterialDesignThemes.Wpf;

using OdantDevApp.Model.ViewModels.Settings;

namespace OdantDevApp.Common;

internal static class ControlEx
{
    public static void ApplyTheming(this Control control)
    {
        control.Resources.SetTheme(AddinSettings.Instance.AppTheme, new());
        AddinSettings.Instance.OnThemeChanged += UpdateThemeHandler;
        control.Unloaded += ControlUnloaded;
        void UpdateThemeHandler(ITheme theme) => control.Resources.SetTheme(theme, new());

        void ControlUnloaded(object sender, EventArgs e)
        {
            ((Control)sender).Unloaded -= ControlUnloaded;
            AddinSettings.Instance.OnThemeChanged -= UpdateThemeHandler;
        }
    }

    public static void SaveInvoke(this Control control, Action action)
    {
        if (control.Dispatcher.CheckAccess())
        {
            action.Invoke();
        }
        else
        {
            control?.Dispatcher.Invoke(action);
        }
    }
}
