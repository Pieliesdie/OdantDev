using MaterialDesignThemes.Wpf;
using OdantDevApp.Common;
using OdantDevApp.Dialogs;

namespace OdantDevApp.Logger;

internal class SnackbarLogger(Snackbar snackbar, LogLevel minLogLevel, string categoryName) : ILogger
{
    private Snackbar Snackbar { get; } = snackbar;

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private static readonly IDisposable emptyScope = new NullScope();

    private void ShowInSnackbar(LogLevel logLevel, string message, Exception? exception = null)
    {
        if (!Snackbar.IsVisible)
        {
            return;
        }

        var messagePrefix = logLevel switch
        {
            LogLevel.Critical => "[CRITICAL]",
            LogLevel.Error => "[ERR]",
            LogLevel.Warning => "[WARN]",
            LogLevel.Information => string.Empty,
            LogLevel.Debug => "[DEBUG]",
            LogLevel.Trace => "[TRACE]",
            _ => string.Empty
        };
        messagePrefix = messagePrefix == string.Empty
            ? string.Empty
            : $"{messagePrefix}: ";

        var content = $"{messagePrefix}{message}";
        var actionContent = exception is null ? "Copy" : $"Copy{Environment.NewLine}stacktrace";
        var actionParameter = exception is null ? message : exception.ToString();

        Snackbar.SaveInvoke(() => SnackbarEnqueue(content, actionContent, actionParameter));
        return;

        void SnackbarEnqueue(object sbContent, object? sbActionContent, string? sbActionArgument)
        {
            Snackbar.MessageQueue?.Enqueue(
                sbContent,
                sbActionContent,
                static s =>
                {
                    Clipboard.Clear();
                    Clipboard.SetText(s);
                },
                sbActionArgument
            );
        }
    }

    private static void ShowErrorMessageBox(LogLevel logLevel, string message, Exception? exception = null)
    {
        if (string.IsNullOrEmpty(message))
        {
            message = "Unknown error";
        }

        Application.Current.Dispatcher.Invoke(() =>
            MessageDialog.Show(
                message,
                logLevel.ToString(),
                MessageDialogIcon.Error,
                (_) => Clipboard.SetText(exception is not null ? $"{message}{Environment.NewLine}{exception}" : message)
            )
        );
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        switch (logLevel)
        {
            case LogLevel.Critical:
                ShowErrorMessageBox(logLevel, message, exception);
                break;
            case LogLevel.Error:
            case LogLevel.Warning:
            case LogLevel.Information:
            case LogLevel.Debug:
            case LogLevel.Trace:
                ShowInSnackbar(logLevel, message, exception);
                break;
            case LogLevel.None:
            default:
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel >= minLogLevel;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return emptyScope;
    }
}

internal sealed class SnackbarLogger<T>(Snackbar snackbar, LogLevel minLogLevel)
    : SnackbarLogger(snackbar, minLogLevel, typeof(T).FullName ?? typeof(T).Name), ILogger<T>;