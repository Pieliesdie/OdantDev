using System.Collections.Concurrent;
using MaterialDesignThemes.Wpf;

namespace OdantDevApp.Logger;

internal sealed class SnackbarLoggerFactory(Snackbar snackbar, LogLevel minLogLevel) : ILoggerFactory
{
    private readonly ConcurrentDictionary<string, ILogger> loggers = new();

    public void AddProvider(ILoggerProvider provider)
    {
        // Не используется — можно игнорировать
    }

    public ILogger CreateLogger(string categoryName)
    {
        return loggers.GetOrAdd(categoryName, name => new SnackbarLogger(snackbar, minLogLevel, name));
    }

    public void Dispose()
    {
        loggers.Clear();
    }
}