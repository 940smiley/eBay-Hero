using Microsoft.Extensions.Logging;
using System.IO;

namespace InventoryPhotoOps.App.Logging;

public sealed class FileLoggerProvider(string logDirectory) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLogger(logDirectory, categoryName);
    public void Dispose() { }

    private sealed class FileLogger(string logDirectory, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            Directory.CreateDirectory(logDirectory);
            var path = Path.Combine(logDirectory, $"InventoryPhotoOps_{DateTimeOffset.Now:yyyyMMdd}.log");
            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {categoryName} {formatter(state, exception)} {exception}";
            File.AppendAllLines(path, [line]);
        }
    }
}
