using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Storage;
using Coordinator.SignalR;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Coordinator
{
    class Worker : IHostedService
    {
        private SecretClient _secretClient;
        private PerfStorageProvider _perfStorageProvider;
        private KubeCtlHelper _kubeCtlHelper;
        private AksHelper _aksHelper;
        private SignalRHelper _signalRHelper;

        public Worker(SecretClient secretClient, PerfStorageProvider perfStorageProvider, KubeCtlHelper kubeCtlHelper, AksHelper aksHelper, SignalRHelper signalRHelper)
        {
            _secretClient = secretClient;
            _perfStorageProvider = perfStorageProvider;
            _kubeCtlHelper = kubeCtlHelper;
            _aksHelper = aksHelper;
            _signalRHelper = signalRHelper;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var perfStorage = await _perfStorageProvider.GetPerfStorageAsync();
            var queue = await perfStorage.GetQueueAsync<string>(PerfConfig.Queue.PORTAL_JOB);
            await foreach (var message in queue.Consume())
            {
                // do the job
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromCanceled(cancellationToken);
        }
    }
}
