using Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class AspNetSignalRHubConnection : IHubConnectionAdapter
    {
        private HubConnection _hubConnection;
        private IHubProxy _hubProxy;
        private IClientTransport _clientTransport;
        private string _transport;
        private SignalREnums.ConnectionInternalStat _stat = SignalREnums.ConnectionInternalStat.Init;

        public event Func<Exception, Task> Closed
        {
            add
            {
                _hubConnection.Closed += () =>
                {
                    value.Invoke(null);
                };
            }
            remove
            {
                _hubConnection.Closed -= () =>
                {
                    value.Invoke(null);
                };
            }
        }

        public AspNetSignalRHubConnection(HubConnection hubConnection, string hubName, string transport)
        {
            _hubConnection = hubConnection;
            _hubProxy = _hubConnection.CreateHubProxy(hubName);
            _transport = transport;
            /*
            _hubConnection.Closed += () =>
            {
                Console.WriteLine("Connection closed");
            };
            _hubConnection.Error += e =>
            {
                Console.WriteLine(e.Message);
            };
            _hubConnection.TraceLevel = TraceLevels.All;
            _hubConnection.TraceWriter = Console.Out;
            */
        }

        public IDisposable On<T1>(string methodName, Action<T1> handler)
        {
            return _hubProxy.On(methodName, handler);
        }

        public IDisposable On(string methodName, Action handler)
        {
            return _hubProxy.On(methodName, handler);
        }

        public Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default)
        {
            return _hubProxy.Invoke(methodName, arg1).OrTimeout();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            _clientTransport = createClientTransport(_transport);
            await _hubConnection.Start(_clientTransport).OrTimeout();
            _stat = SignalREnums.ConnectionInternalStat.Active;
        }

        private IClientTransport createClientTransport(string transport)
        {
            switch (transport)
            {
                case "Websockets":
                    return new WebSocketTransport();
                case "LongPolling":
                    return new LongPollingTransport();
                case "ServerSentEvents":
                    return new ServerSentEventsTransport();
                default:
                    throw new NotSupportedException($"wrong transport type {transport}");
            }
        }

        public Task StopAsync()
        {
            try
            {
                // If connection fails to start, its internal state is not complete.
                // Exception will thrown if invoking Stop.
                _hubConnection.Stop();
                _stat = SignalREnums.ConnectionInternalStat.Stopped;
            }
            catch (Exception e)
            {
                Log.Error($"Fail to stop: {e.Message}");
            }
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _stat = SignalREnums.ConnectionInternalStat.Disposed;
            try
            {
                // If connection fails to start, its internal state is not complete.
                // Exception will thrown if invoking Dispose.
                _hubConnection.Dispose();
            }
            catch (Exception e)
            {
                Log.Error($"Fail to dispose: {e.Message}");
            }
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(string method)
        {
            return _hubProxy.Invoke<T>(method);
        }

        public Task SendAsync(string methodName, CancellationToken cancellationToken = default)
        {
            return _hubProxy.Invoke(methodName).OrTimeout();
        }

        public SignalREnums.ConnectionInternalStat GetStat()
        {
            return _stat;
        }
    }
}
