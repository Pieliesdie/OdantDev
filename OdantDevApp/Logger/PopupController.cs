﻿using System;
using System.Windows;

using MaterialDesignThemes.Wpf;

using OdantDevApp.Common;
using OdantDevApp.Dialogs;

namespace OdantDev.Model;
internal class PopupController : ILogger
{
    private readonly Snackbar _snackbar;
    private readonly Action<string> _copyAction = (s) =>  { Clipboard.Clear(); Clipboard.SetText(s); };
    public void Info(string message)
    {
        if (_snackbar.IsVisible.Not()) return;
        var enqueueAction = new Action(() => _snackbar?.MessageQueue?.Enqueue(message, "Copy", _copyAction, message));
        _snackbar.SaveInvoke(enqueueAction);
    }

    public void Error(string message)
    {
        if (string.IsNullOrEmpty(message))
            message = "Unknown error";
        Application.Current.Dispatcher.Invoke(() => MessageDialog.Show(message));
    }

    public void Exception(Exception ex)
    {
        if (_snackbar.IsVisible.Not()) return;

        var enqueueAction = new Action(() =>
            _snackbar?.MessageQueue?.Enqueue(
                $"Unhandeled exception: {ex.Message}",
                $"Copy{Environment.NewLine}stacktrace",
                _copyAction,
                ex.ToString()));
        _snackbar.SaveInvoke(enqueueAction);
    }

    public PopupController(Snackbar snackbar)
    {
        this._snackbar = snackbar ?? throw new NullReferenceException(nameof(snackbar));
    }
}
