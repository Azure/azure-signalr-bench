using System;
using System.Collections.Generic;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SignalRUpstream.Controllers
{
    [ApiController]
    public class NegotiateController : ControllerBase
    {
        private readonly IServiceManager[] _serviceManager;
        private ILogger<UpStreamController> _logger;

        public NegotiateController(ILogger<UpStreamController> logger, IConfiguration configuration)
        {
            _logger = logger;
            var endpoints = SignalRManagedIdentity.ParseEndpoints(
                configuration[PerfConstants.ConfigurationKeys.ConnectionString]);
            var credential = SignalRManagedIdentity.CreateCredential(
                configuration[PerfConstants.ConfigurationKeys.MsiAppId]);
            _serviceManager = new IServiceManager[endpoints.Length];
            for (var i = 0; i < endpoints.Length; i++)
            {
                _serviceManager[i] = new ServiceManagerBuilder()
                    .WithOptions(o => o.ServiceEndpoints = new[] {new ServiceEndpoint(endpoints[i], credential)})
                    .Build();
            }
        }

        [HttpPost("{hub}/negotiate")]
        public ActionResult Index(string hub)
        {
            var user = Request.Headers["user"];
            _logger.LogInformation($"user :{user} try to negotiate hub: {hub}");
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User ID is null or empty.");
            }
            var index = StaticRandom.Next(_serviceManager.Length);
            return new JsonResult(new Dictionary<string, string>()
            {
                {"url", _serviceManager[index].GetClientEndpoint(hub)},
                {"accessToken", _serviceManager[index].GenerateClientAccessToken(hub, user)}
            });
        }
    }
}