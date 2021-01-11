using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Portal
{
    public class ClusterState
    {
        private SecretClient _secretClient;

        public string Location { get; private set; }
        public bool PPEEnabled { get; private set; } = false;

        public ClusterState(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public async Task Init()
        {
            var locationTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.LocationKey);
            var spTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PPESubscriptionKey);
            Location = (await locationTask).Value.Value;
            try
            {
                PPEEnabled = (await spTask).Value != null;
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}