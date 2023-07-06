using MaterialDesignThemes.Wpf;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OdantDev.Model;
internal class PopupController : ILogger
{
    private readonly Snackbar snackbar;
    public void Info(string message)
    {
        if (snackbar.IsVisible.Not()) return;
        var copyAction = new Action<string>(
            (string s) =>
            {
                Clipboard.Clear();
                Clipboard.SetText(s);
            });
        var enqueueAction = new Action(() => snackbar.MessageQueue.Enqueue(message, "Copy", copyAction, message));
        if (snackbar.Dispatcher.CheckAccess())
        {
            enqueueAction.Invoke();
        }
        else
        {
            snackbar?.Dispatcher.Invoke(enqueueAction);
        }
    }

    public void Error(string message)
    {
        Info(message);
    }

    public PopupController(Snackbar snackbar)
    {
        this.snackbar = snackbar ?? throw new NullReferenceException(nameof(snackbar));
    }
}
