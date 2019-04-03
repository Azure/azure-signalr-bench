using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Azure.SignalR.PerfTest.AppServer
{
    public class TimedLoggerFactory : ILoggerFactory
    {
        private ILoggerFactory _loggerFactory;
        private volatile bool _disposed;

        protected virtual bool CheckDisposed() => _disposed;

        public TimedLoggerFactory()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(config =>
            {
                config.AddConsole();
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            _loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }
            var logger = _loggerFactory.CreateLogger(categoryName);
            return new TimedLogger(logger);
        }

        public ILogger CreateLogger<T>()
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }
            var logger = _loggerFactory.CreateLogger<T>();
            return new TimedLogger<T>(logger);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
    }
}
