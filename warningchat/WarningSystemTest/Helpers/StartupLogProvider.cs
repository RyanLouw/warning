using Microsoft.Extensions.Logging;

namespace WarningSystemTest.Helpers;

internal sealed class StartupLogProvider(string path, string testKey) : ILoggerProvider
{
    private readonly object _gate = new();
    public ILogger CreateLogger(string categoryName) => new StartupLogger(this, categoryName);
    public void Dispose() { }
    private void Write(string message)
    {
        message = message.Replace(testKey, "[test-key]", StringComparison.Ordinal);
        lock (_gate) File.AppendAllText(path, message + Environment.NewLine);
    }

    private sealed class StartupLogger(StartupLogProvider owner, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Information;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level)) return;
            var message = $"{DateTime.UtcNow:O} {level} {category}: {formatter(state, exception)} {exception}";
            owner.Write(message);
        }
    }
}
