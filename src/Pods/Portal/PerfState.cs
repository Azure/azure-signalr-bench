using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Logging;
using Portal.Entity;

namespace Portal
{
    public class PerfState
    {
        private readonly SecretClient _secretClient;
        private readonly IPerfStorage _perfStorage;
        private readonly ILogger<PerfState> _logger;

        public PerfState(SecretClient secretClient, IPerfStorage perfStorage, ILogger<PerfState> logger)
        {
            _secretClient = secretClient;
            _perfStorage = perfStorage;
            _logger = logger;
        }

        public string Location { get; private set; }
        public bool PPEEnabled { get; private set; }

        public string PPELocation { get; private set; } = "";
        public X509Certificate2 AuthCert { get; private set; }
        public string DefaultHostLocation { get; private set; } 
        public IList<string> HostLocations { get; private set; } = new List<string>();

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

            await Refresh();

            _ = Task.Run(async () =>
            {
                await Task.Delay(60 * 1000);
                await Refresh();
            });
        }

        public string GetQueueName(string? targetLocation)
        {
            if (string.IsNullOrEmpty(targetLocation))
            {
                if (string.IsNullOrEmpty(DefaultHostLocation))
                {
                    return PerfConstants.QueueNames.PortalJob;
                }
                else
                {
                    return $"{PerfConstants.QueueNames.PortalJob}-{DefaultHostLocation}";
                }
            }
            else
            {
                return $"{PerfConstants.QueueNames.PortalJob}-{targetLocation}";
            }
        }

        private async Task Refresh()
        {
            try
            {
                var defaultLocationTask = _secretClient.GetSecretAsync(PerfConstants.KeyVaultKeys.DefaultHostLocationKey);
                DefaultHostLocation = (await defaultLocationTask).Value.Value;
                _logger.LogInformation($"Default host location: {DefaultHostLocation}");
            }catch (Exception e)
            {
                // _logger.LogError(e,"Default host location not found");
            }
            
            var table = await _perfStorage.GetTableAsync<HostCluster>(PerfConstants.TableNames.HostCluster);
            var list=await table.QueryAsync(table.Rows).ToListAsync();
            HostLocations = list.Select(x => x.Location).ToList();
        }
    }
}