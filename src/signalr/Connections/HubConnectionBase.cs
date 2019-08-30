using Common;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class HubConnectionBase
    {
        protected readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1);
        protected readonly object _lock = new object();
        protected long _stat = (long)SignalREnums.ConnectionInternalStat.Init;

        protected bool _born = false;

        protected string InternalConnectionId { get; set; }

        protected long InternalBigMessageLatencyCount { get; set; }

        // The timestamp of 1st time connection connected, never changed
        protected long ConnectionBornTime { get; set; }

        // The timestamp when starting connecting
        protected long StartConnectingTime { get; set; }

        // The timestamp when connected
        protected long ConnectedTime { get; set; }

        // Last disconnected timestamp
        protected long LastDisconnectedTime { get; set; }

        // The period of disconnected stat
        protected long DownTime { get; set; }

        protected void ResetTimestamps()
        {
            StartConnectingTime = -1;
            ConnectedTime = -1;
            LastDisconnectedTime = -1;
            ConnectionBornTime = -1;
            DownTime = 0;
        }

        protected void LockedUpdate()
        {
            lock (_lock)
            {
                ConnectedTime = Util.Timestamp();
                if (LastDisconnectedTime != -1)
                {
                    DownTime += ConnectedTime - LastDisconnectedTime;
                }
            }
        }

        protected Task MarkAsStopped()
        {
            StartConnectingTime = -1;
            ConnectedTime = -1;
            LastDisconnectedTime = Util.Timestamp();
            Volatile.Write(ref _stat, (long)SignalREnums.ConnectionInternalStat.Stopped);
            return Task.CompletedTask;
        }

        protected SignalREnums.ConnectionInternalStat GetStatCore()
        {
            return (SignalREnums.ConnectionInternalStat)Volatile.Read(ref _stat);
        }

        protected Task OnClosedCore(Exception e)
        {
            if (GetStatCore() != SignalREnums.ConnectionInternalStat.Stopped)
            {
                // AspNet SignalR does not pass exception object
                if (e != null)
                {
                    Log.Error($"connection closed for {e.Message}");
                }
                else
                {
                    Log.Error("connection closed");
                }
                return MarkAsStopped();
            }
            return Task.CompletedTask;
        }
    }
}
