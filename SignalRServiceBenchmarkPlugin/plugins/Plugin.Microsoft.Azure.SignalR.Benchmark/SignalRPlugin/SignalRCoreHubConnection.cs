using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SignalRCoreHubConnection : IHubConnectionAdapter
    {
        private HubConnection _hubConnection;

        public SignalRCoreHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
        }

        public event Func<Exception, Task> Closed
        {
            add
            {
                _hubConnection.Closed += value;
            }
            remove
            {
                _hubConnection.Closed -= value;
            }
        }

        public Task DisposeAsync()
        {
            return _hubConnection.DisposeAsync();
        }

        public Task<T> InvokeAsync<T>(string method)
        {
            return _hubConnection.InvokeAsync<T>(method);
        }

        public IDisposable On<T1>(string methodName, Action<T1> handler)
        {
            return _hubConnection.On<T1>(methodName, handler);
        }

        public IDisposable On(string methodName, Action handler)
        {
            return _hubConnection.On(methodName, handler);
        }

        public Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default)
        {
            return _hubConnection.SendAsync(methodName, arg1, cancellationToken);
        }

        public Task SendAsync(string methodName, CancellationToken cancellationToken = default)
        {
            return _hubConnection.SendAsync(methodName, cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return _hubConnection.StartAsync(cancellationToken);
        }

        public Task StopAsync()
        {
            return _hubConnection.StopAsync();
        }
    }
}
