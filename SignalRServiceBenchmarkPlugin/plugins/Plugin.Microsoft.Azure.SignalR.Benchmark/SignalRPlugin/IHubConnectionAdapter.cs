using System;
using System.Threading;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public interface IHubConnectionAdapter
    {
        IDisposable On<T1>(string methodName, Action<T1> handler);

        IDisposable On(string methodName, Action handler);

        Task SendAsync(string methodName, CancellationToken cancellationToken = default);

        Task SendAsync(string methodName, object arg1, CancellationToken cancellationToken = default);

        Task<T> InvokeAsync<T>(string method);

        Task StartAsync(CancellationToken cancellationToken = default);

        Task StopAsync();

        Task DisposeAsync();

        SignalREnums.ConnectionInternalStat GetStat();

        event Func<Exception, Task> Closed;
    }
}
