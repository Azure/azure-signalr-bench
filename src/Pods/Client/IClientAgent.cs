using System.Threading;
using System.Threading.Tasks;

namespace Azure.SignalRBench.Client
{
    public interface IClientAgent
    {
        public ClientAgentContext Context { get; }

        public Task StartAsync(CancellationToken cancellationToken);

        public Task StopAsync();

        public Task EchoAsync(string payload);

        public Task BroadcastAsync(string payload);

        public Task GroupBroadcastAsync(string group, string payload);

        public Task JoinGroupAsync();
    }
}
