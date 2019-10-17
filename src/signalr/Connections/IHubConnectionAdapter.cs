using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public interface IHubConnectionAdapter
    {
        IDisposable On<T1>(string methodName, Action<T1> handler);

        IDisposable On(string methodName, Action handler);

        Task SendAsync(string methodName, CancellationToken cancellationToken = default);

        Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default);

        Task<ChannelReader<TResult>> StreamAsChannelAsync<TResult>(string methodName, object arg1, object arg2, CancellationToken cancellationToken = default);

        Task<T> InvokeAsync<T>(string method);

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync();

        Task DisposeAsync();

        Task OnClosed(Exception e);

        event Func<Exception, Task> Closed;

        SignalREnums.ConnectionInternalStat GetStat();

        long ConnectionBornTimestamp { get; set; }

        long ConnectedTimestamp { get; set; }

        long DowntimePeriod { get; set; }

        long LastDisconnectedTimestamp { get; set; }

        long StartConnectingTimestamp { get; set; }

        void UpdateTimestampWhenConnected();
    }
}
