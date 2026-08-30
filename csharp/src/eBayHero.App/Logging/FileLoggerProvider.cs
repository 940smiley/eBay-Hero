using eBayHero.Core.Services;
using Microsoft.Extensions.Logging;
using System.IO;

namespace eBayHero.App.Logging;

public sealed class FileLoggerProvider(string logDirectory, ISecretRedactor? redactor = null) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new FileLogger(logDirectory, categoryName, redactor);
    public void Dispose() { }

    private sealed class FileLogger(string logDirectory, string categoryName, ISecretRedactor? redactor) : ILogger
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
            var path = Path.Combine(logDirectory, $"eBayHero_{DateTimeOffset.Now:yyyyMMdd}.log");
            var line = $"{DateTimeOffset.Now:O} [{logLevel}] {categoryName} {formatter(state, exception)} {exception}";
            if (redactor is not null)
            {
                line = redactor.Redact(line);
            }

            File.AppendAllLines(path, [line]);
        }
    }
}

