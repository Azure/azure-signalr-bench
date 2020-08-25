using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Common
{
    public class BlobLoggerProvider : ILoggerProvider
    {
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly ConcurrentQueue<LogItem> _queue = new ConcurrentQueue<LogItem>();
        private readonly ConcurrentDictionary<string, BlobLogger> _loggers = new ConcurrentDictionary<string, BlobLogger>();
        private readonly TaskCompletionSource<bool> _stopped = new TaskCompletionSource<bool>();

        private readonly BlobContainerClient _client;
        private readonly string _prefix;
        private readonly string _suffix;
        private bool _isStopping;
        private AppendBlobClient? _current;
        private DateTime _expiresAt;

        public BlobLoggerProvider(string prefix, string suffix, string connectionString)
        {
            var serviceClient = new BlobServiceClient(connectionString);
            _prefix = prefix;
            _suffix = suffix;
            _client = serviceClient.GetBlobContainerClient("logs");
            _ = StartDequeue();
        }

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, new BlobLogger(this, categoryName));

        public void Dispose()
        {
            Volatile.Write(ref _isStopping, true);
            _stopped.Task.Wait();
        }

        private void Enqueue(LogItem line)
        {
            if (!_isStopping)
            {
                _queue.Enqueue(line);
            }
        }

        private async Task StartDequeue()
        {
            const int FlushSize = 16 * 1024;
            while (true)
            {
                if (_queue.TryDequeue(out var item))
                {
                    if (!await EnsureBlobSafeAsync(item.EventTime))
                    {
                        await Task.Delay(1);
                        continue;
                    }
                    using var ms = new MemoryStream();
                    using (var sw = new StreamWriter(ms, Utf8WithoutBom, 4096, true))
                    {
                        while (true)
                        {
                            sw.WriteLine(JsonConvert.SerializeObject(item));
                            if (ms.Length >= FlushSize)
                            {
                                break;
                            }
                            item = await TryTakeAsync();
                            if (item == null)
                            {
                                break;
                            }
                        }
                    }
                    ms.Seek(0, SeekOrigin.Begin);
                    await WriteBlobAsync(ms);
                    continue;
                }
                else
                {
                    if (_isStopping)
                    {
                        break;
                    }
                    await Task.Delay(1);
                }
            }
            _stopped.TrySetResult(true);
        }

        private async ValueTask<LogItem?> TryTakeAsync()
        {
            const int MaxDelay = 10;
            for (int i = 0; i < MaxDelay; i++)
            {
                if (_queue.TryDequeue(out var item))
                {
                    return item;
                }
                else
                {
                    await Task.Delay(1);
                }
            }

            return null;
        }

        private async ValueTask<bool> EnsureBlobSafeAsync(DateTime eventTime)
        {
            try
            {
                await EnsureBlobAsync(eventTime);
                return true;
            }
            catch (Exception)
            {
                // Abandon logs.
                while (_queue.TryDequeue(out _))
                    ;
                return false;
            }
        }

        private async ValueTask EnsureBlobAsync(DateTime eventTime)
        {
            if (_expiresAt < eventTime)
            {
                var name = $"{_prefix}{eventTime:yyyyMMdd_HHmmss}{_suffix}";
                _current = _client.GetAppendBlobClient(name);
                await _current.CreateAsync();
                var temp = eventTime.AddHours(1);
                _expiresAt = new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, 0, 0, eventTime.Kind);
            }
        }

        private async Task WriteBlobAsync(MemoryStream ms)
        {
            while (true)
            {
                try
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    await _current.AppendBlockAsync(ms);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    return;
                }
                catch (Exception)
                {
                }
                try
                {
                    await EnsureBlobAsync(DateTime.UtcNow);
                }
                catch (Exception)
                {
                    // Abandon current logs.
                    return;
                }
            }
        }

        private sealed class BlobLogger : ILogger
        {
            private readonly BlobLoggerProvider _provider;
            private readonly string _categoryName;

            public BlobLogger(BlobLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable BeginScope<TState>(TState state) => Disposable.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= _provider.LogLevel;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                var ext = new Dictionary<string, JToken>();
                if (state is IEnumerable<KeyValuePair<string, object>> pairs)
                {
                    foreach (var pair in pairs)
                    {
                        ext[pair.Key] = JToken.FromObject(pair.Value);
                    }
                }
                else if (state != null)
                {
                    ext["State"] = JToken.FromObject(state);
                }
                _provider.Enqueue(
                    new LogItem
                    {
                        EventTime = DateTime.UtcNow,
                        EventId = eventId.Id,
                        EventName = eventId.Name,
                        ThreadId = Thread.CurrentThread.ManagedThreadId,
                        Logger = _categoryName,
                        Text = formatter(state, exception),
                        Exception = exception?.ToString(),
                        Extensions = ext,
                    });
            }

            private sealed class Disposable : IDisposable
            {
                public static readonly Disposable Instance = new Disposable();

                public void Dispose()
                {
                }
            }
        }

        private sealed class LogItem
        {
            public DateTime EventTime { get; set; }
            public int EventId { get; set; }
            public string EventName { get; set; } = string.Empty;
            public int ThreadId { get; set; }
            public string Logger { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public string? Exception { get; set; }
            [JsonExtensionData(ReadData = false, WriteData = true)]
            public Dictionary<string, JToken>? Extensions { get; set; }
        }
    }
}
