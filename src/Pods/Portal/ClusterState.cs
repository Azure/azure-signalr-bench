using System;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;

namespace Portal
{
    public class ClusterState
    {
        private readonly SecretClient _secretClient;

        public ClusterState(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public string Location { get; private set; }
        public bool PPEEnabled { get; private set; }

        public string PPELocation { get; private set; } = "";

        public async Task Init()
        {
            var locationTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.LocationKey);
            var ppeTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PPELocationKey);
            Location = (await locationTask).Value.Value;
            try
            {
                PPEEnabled = (await ppeTask).Value != null;
                PPELocation = (await ppeTask).Value.Value;
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}