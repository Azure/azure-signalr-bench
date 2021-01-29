using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Microsoft.Extensions.Logging;
using Portal.Controllers;

namespace Portal
{
    public class ClusterState
    {
        private readonly SecretClient _secretClient;
        private readonly ILogger<ClusterState> _logger;

        public ClusterState(SecretClient secretClient, ILogger<ClusterState> logger)
        {
            _secretClient = secretClient;
            _logger = logger;
        }

        public string Location { get; private set; }
        public bool PPEEnabled { get; private set; }

        public string PPELocation { get; private set; } = "";
        public X509Certificate2 AuthCert { get; private set; }

        public async Task Init()
        {
            var locationTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.LocationKey);
            var ppeTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.PPELocationKey);
            var certTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.EncryptCert);
            Location = (await locationTask).Value.Value;
            try
            {
                PPEEnabled = (await ppeTask).Value != null;
                PPELocation = (await ppeTask).Value.Value;
                var base64 = (await certTask).Value.Value;
                AuthCert= new X509Certificate2( Convert.FromBase64String(base64));
            }
            catch (Exception e)
            {
                // ignored
                _logger.LogError(e,"Cluster state init error");
            }
            
        }
    }
}