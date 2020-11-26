using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("Home")]
    [ApiController]
    [Authorize]
    public class HomeController : ControllerBase
    {
        private IPerfStorage _perfStorage;
        private SecretClient _secretClient;
        private static string? location;
        private static string? k8sUrl;

        public HomeController(IPerfStorage perfStorage,SecretClient secretClient)
        {
            _perfStorage = perfStorage;
            _secretClient = secretClient;
        }

        [HttpGet("info")]
        public async Task<JObject> Get()
        {
            JObject info=new JObject();
            List<Task> tasks=new List<Task>();
            if (location == null)
            {
                tasks.Add(Task.Run(async () =>
                {
                 var res= await  _secretClient.GetSecretAsync(Constants.KeyVaultKeys.LocationKey);
                 location = res.Value.Value;
                 info.Add(Constants.KeyVaultKeys.LocationKey,location); 
                })); 
            }
            else
            {
                info.Add(Constants.KeyVaultKeys.LocationKey,location); 
            }

            await Task.WhenAll(tasks);
            return info;
        }
    }
}
