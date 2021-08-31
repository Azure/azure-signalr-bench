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
        Task EchoAsync(string payload);
        Task SendToClientAsync(int index, string payload);
        Task BroadcastAsync(string payload);
        Task GroupBroadcastAsync(string group, string payload);
        Task JoinGroupAsync();
    }
}