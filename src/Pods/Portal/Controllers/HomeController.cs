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
    public class HomeController : ControllerBase
    {
        private IPerfStorage _perfStorage;
        private SecretClient _secretClient;
        private ILogger<HomeController> _logger;
        private static string? location;
        private static string? k8sUrl;

        
        public HomeController(IPerfStorage perfStorage,SecretClient secretClient,ILogger<HomeController> logger)
        {
            _perfStorage = perfStorage;
            _secretClient = secretClient;
            _logger = logger;
        }

        [HttpGet("info")]
        public async Task<string> Get()
        {
            return "{\"info\":\"Todo\"}";
        }
    }
}
