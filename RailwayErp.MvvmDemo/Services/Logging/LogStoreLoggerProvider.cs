using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RailwayErp.MvvmDemo.Models;

namespace RailwayErp.MvvmDemo.Services.Logging;

/// <summary>
/// Провайдер Microsoft.Extensions.Logging, який пише повідомлення застосунку в <see cref="ILogStore"/>.
/// Підключається у MauiProgram поруч із AddDebug(): той самий ILogger&lt;T&gt; пише
/// і у вікно Output (Debug), і на сторінку «Журнал», і у файл lifecycle.log.
/// </summary>
public sealed class LogStoreLoggerProvider(ILogStore store) : ILoggerProvider
{
    /// <summary>Пишемо лише власні категорії, щоб у журнал не потрапляв «шум» фреймворку.</summary>
    private const string CategoryPrefix = "RailwayErp";

    public ILogger CreateLogger(string categoryName) =>
        categoryName.StartsWith(CategoryPrefix, StringComparison.Ordinal)
            ? new LogStoreLogger(categoryName, store)
            : NullLogger.Instance;

    public void Dispose()
    {
    }

    private sealed class LogStoreLogger(string category, ILogStore store) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);
            if (exception is not null)
                message += $" | {exception.GetType().Name}: {exception.Message}";

            store.Add(new LogEntry(DateTimeOffset.Now, logLevel, category, message));
        }
    }
}
