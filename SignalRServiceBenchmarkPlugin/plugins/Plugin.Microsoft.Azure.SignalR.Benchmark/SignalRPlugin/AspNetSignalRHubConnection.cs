using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.Transports;
using System;
using System.Collections.Generic;
using System.Text;
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
        public event Func<Exception, Task> Closed;

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
            return _hubProxy.Invoke(methodName, arg1);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            _clientTransport = createClientTransport(_transport);
            return _hubConnection.Start(_clientTransport);
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
            _hubConnection.Stop();
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            _hubConnection.Dispose();
            return Task.CompletedTask;
        }

        public Task<T> InvokeAsync<T>(string method)
        {
            return _hubProxy.Invoke<T>(method);
        }
    }
}
