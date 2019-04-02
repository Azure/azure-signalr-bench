using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SignalRCoreHubConnection : IHubConnectionAdapter
    {
        private HubConnection _hubConnection;
        private SignalREnums.ConnectionInternalStat _stat = SignalREnums.ConnectionInternalStat.Init;

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
            _stat = SignalREnums.ConnectionInternalStat.Disposed;
            return _hubConnection.DisposeAsync();
        }

        public SignalREnums.ConnectionInternalStat GetStat()
        {
            return _stat;
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

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            await _hubConnection.StartAsync(cancellationToken);
            _stat = SignalREnums.ConnectionInternalStat.Active;
        }

        public Task StopAsync()
        {
            _stat = SignalREnums.ConnectionInternalStat.Stopped;
            return _hubConnection.StopAsync();
        }
    }
}
