using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Serilog;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.Internals
{
    [ApiController]
    public class NegotiationController : ControllerBase
    {
        private readonly IServiceManager _serviceManager;

        public NegotiationController(IServiceManager serviceManager)
        {
            _serviceManager = serviceManager;
        }

        [HttpPost("{hub}/negotiate")]
        public ActionResult Index(string hub, string user)
        {
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User ID is null or empty.");
            }

            return new JsonResult(new Dictionary<string, string>()
            {
                { "url", _serviceManager.GetClientEndpoint(hub) },
                { "accessToken", _serviceManager.GenerateClientAccessToken(hub, user, lifeTime = new TimeSpan(10, 0, 0, 0, 0)) }
            });
        }
    }
}
