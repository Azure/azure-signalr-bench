using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client.ClientAgent
{
    public interface IClientAgent
    {
        ClientAgentContext Context { get; }
        int GlobalIndex { get; }
        Task StartAsync(CancellationToken cancellationToken);
        Task StopAsync();
        Task EchoAsync(string payload, CancellationToken cancellationToken = default);
        Task SendToClientAsync(int index, string payload, CancellationToken cancellationToken = default);
        Task BroadcastAsync(string payload, CancellationToken cancellationToken = default);
        Task GroupBroadcastAsync(string group, string payload, CancellationToken cancellationToken = default);
        Task JoinGroupAsync();
    }
}