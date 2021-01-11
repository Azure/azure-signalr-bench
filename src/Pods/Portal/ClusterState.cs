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

        public string PPELocation { get; private set; } = "";

        public ClusterState(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

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