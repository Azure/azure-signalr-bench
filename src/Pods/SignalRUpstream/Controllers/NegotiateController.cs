using System;
using System.Collections.Generic;
using Azure.SignalRBench.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SignalRUpstream.Controllers
{
    [ApiController]
    public class NegotiateController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;
        private ILogger<UpStreamController> _logger;

        public NegotiateController(ILogger<UpStreamController> logger, IConfiguration configuration)
        {
            _logger = logger;
            var connectionString = configuration[PerfConstants.ConfigurationKeys.ConnectionString];
            Console.WriteLine($"connecionString:{configuration[PerfConstants.ConfigurationKeys.ConnectionString]}");
            _serviceManager = new ServiceManagerBuilder()
                .WithOptions(o => o.ConnectionString = connectionString)
                .Build();
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
            return new JsonResult(new Dictionary<string, string>()
            {
                {"url", _serviceManager.GetClientEndpoint(hub)},
                {"accessToken", _serviceManager.GenerateClientAccessToken(hub, user)}
            });
        }
    }
}