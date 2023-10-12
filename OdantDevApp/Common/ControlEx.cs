using System;
using System.Windows.Controls;

namespace OdantDevApp.Common;

internal static class ControlEx
{
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
