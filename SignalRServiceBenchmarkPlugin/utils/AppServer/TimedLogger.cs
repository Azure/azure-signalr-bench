using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class TimedLogger<T> : ILogger<T>
    {
        private readonly ILogger _logger;

        public TimedLogger(ILogger logger) => _logger = logger;

        public TimedLogger(ILoggerFactory loggerFactory) : this(new Logger<T>(loggerFactory))
        {
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception,
                (s, ex) => $"[{DateTime.UtcNow:yyyy:MM:dd:HH:mm:ss.fff}]: {formatter(s, ex)}");

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
    }

    public class TimedLogger : ILogger
    {
        private readonly ILogger _logger;

        public TimedLogger(ILogger logger) => _logger = logger;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            _logger.Log(logLevel, eventId, state, exception,
                (s, ex) => $"[{DateTime.UtcNow:yyyy:MM:dd:HH:mm:ss.fff}]: {formatter(s, ex)}");

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);
    }
}
