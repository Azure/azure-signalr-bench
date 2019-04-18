using Common;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class SignalRCoreHubConnection : HubConnectionBase, IHubConnectionAdapter
    {
        private HubConnection _hubConnection;

        public long ConnectionBornTimestamp
        {
            get
            {
                return ConnectionBornTime;
            }
            set
            {
                ConnectionBornTime = value;
            }
        }

        public long ConnectedTimestamp
        {
            get
            {
                return ConnectedTime;
            }
            set
            {
                ConnectedTime = value;
            }
        }

        public long DowntimePeriod
        {
            get
            {
                return DownTime;
            }
            set
            {
                DownTime = value;
            }
        }

        public long LastDisconnectedTimestamp
        {
            get
            {
                return LastDisconnectedTime;
            }
            set
            {
                LastDisconnectedTime = value;
            }
        }

        public long StartConnectingTimestamp
        {
            get
            {
                return StartConnectingTime;
            }
            set
            {
                StartConnectingTime = value;
            }
        }

        public SignalRCoreHubConnection(HubConnection hubConnection)
        {
            _hubConnection = hubConnection;
            ResetTimestamps();
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

        public async Task DisposeAsync()
        {
            Volatile.Write(ref _stat, (long)SignalREnums.ConnectionInternalStat.Disposed);
            await _hubConnection.DisposeAsync();
        }

        public SignalREnums.ConnectionInternalStat GetStat()
        {
            return GetStatCore();
        }

        public async Task<T> InvokeAsync<T>(string method)
        {
            try
            {
                return await _hubConnection.InvokeAsync<T>(method);
            }
            catch
            {
                await MarkAsStopped();
                throw;
            }
        }

        public IDisposable On<T1>(string methodName, Action<T1> handler)
        {
            return _hubConnection.On<T1>(methodName, handler);
        }

        public IDisposable On(string methodName, Action handler)
        {
            return _hubConnection.On(methodName, handler);
        }

        public async Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hubConnection.SendAsync(methodName, arg1, cancellationToken);
            }
            catch
            {
                await MarkAsStopped();
                throw;
            }
        }

        public async Task SendAsync(string methodName, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hubConnection.SendAsync(methodName, cancellationToken);
            }
            catch
            {
                await MarkAsStopped();
                throw;
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (!_connectionLock.Wait(0))
            {
                // avoid multiple try to reconnect
                return;
            }
            try
            {
                StartConnectingTimestamp = Util.Timestamp();
                await _hubConnection.StartAsync(cancellationToken);
                Volatile.Write(ref _stat, (long)SignalREnums.ConnectionInternalStat.Active);
                ConnectedTimestamp = Util.Timestamp();
                if (!_born)
                {
                    _born = true;
                    ConnectionBornTimestamp = Util.Timestamp();
                }
            }
            catch
            {
                await MarkAsStopped();
                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task StopAsync()
        {
            await MarkAsStopped();
            await _hubConnection.StopAsync();
        }

        public async Task OnClosed(Exception e)
        {
            await OnClosedCore(e);
        }

        public void UpdateTimestampWhenConnected()
        {
            LockedUpdate();
        }
    }
}
