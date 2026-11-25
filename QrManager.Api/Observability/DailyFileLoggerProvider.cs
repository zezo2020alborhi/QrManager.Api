using Microsoft.Extensions.Logging;

namespace QrManager.Api.Observability;

public sealed class DailyFileLoggerProvider(string logDirectory) : ILoggerProvider
{
    private readonly object _sync = new();

    public ILogger CreateLogger(string categoryName) => new DailyFileLogger(logDirectory, _sync, categoryName);

    public void Dispose()
    {
    }

    private sealed class DailyFileLogger(string logDirectory, object sync, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            try
            {
                Directory.CreateDirectory(logDirectory);
                var filePath = Path.Combine(logDirectory, $"api-{DateTime.Now:yyyyMMdd}.log");
                var message = formatter(state, exception).Replace(Environment.NewLine, " ");
                var exceptionText = exception is null ? string.Empty : $" | {exception.GetType().Name}: {exception.Message}";
                var line = $"{DateTime.Now:O} [{logLevel}] {categoryName}: {message}{exceptionText}{Environment.NewLine}";
                lock (sync)
                {
                    File.AppendAllText(filePath, line);
                }
            }
            catch
            {
                // Logging must never interrupt QR resolution or API responses.
            }
        }
    }
}
