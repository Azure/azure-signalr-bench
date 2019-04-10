using Common;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class AspNetSignalRHubConnection : HubConnectionBase, IHubConnectionAdapter
    {
        private HubConnection _hubConnection;
        private IHubProxy _hubProxy;
        private IClientTransport _clientTransport;
        private string _transport;

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
            ResetTimestamps();

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

        public async Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default)
        {
            try
            {
                await _hubProxy.Invoke(methodName, arg1).OrTimeout();
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
                _clientTransport = createClientTransport(_transport);
                StartConnectingTimestamp = Util.Timestamp();
                await _hubConnection.Start(_clientTransport).OrTimeout();
                Volatile.Write(ref _stat, (long)SignalREnums.ConnectionInternalStat.Active);
                ConnectedTimestamp = Util.Timestamp();
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

        public async Task StopAsync()
        {
            try
            {
                // If connection fails to start, its internal state is not complete.
                // Exception will thrown if invoking Stop.
                _hubConnection.Stop();
            }
            catch (Exception e)
            {
                Log.Error($"Fail to stop: {e.Message}");
            }
            await MarkAsStopped();
        }

        public Task DisposeAsync()
        {
            Volatile.Write(ref _stat, (long)SignalREnums.ConnectionInternalStat.Disposed);
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
            finally
            {
            }
            return Task.CompletedTask;
        }

        public async Task<T> InvokeAsync<T>(string method)
        {
            try
            {
                return await _hubProxy.Invoke<T>(method);
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
                await _hubProxy.Invoke(methodName).OrTimeout();
            }
            catch
            {
                await MarkAsStopped();
                throw;
            }
        }

        public SignalREnums.ConnectionInternalStat GetStat()
        {
            return GetStatCore();
        }

        public Task OnClosed(Exception e)
        {
            return OnClosedCore(e);
        }

        public void UpdateTimestampWhenConnected()
        {
            LockedUpdate();
        }
    }
}
