using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.Logging;

namespace SignalRUpstream.Controllers
{
    [ApiController]
    public class NegotiateController : ControllerBase
    {
        private readonly EndpointHubContexts _hubContexts;
        private readonly ILogger<NegotiateController> _logger;

        public NegotiateController(EndpointHubContexts hubContexts, ILogger<NegotiateController> logger)
        {
            _hubContexts = hubContexts;
            _logger = logger;
        }

        [HttpPost("{hub}/negotiate")]
        public async Task<ActionResult> Index(string hub)
        {
            string user = Request.Headers["user"];
            _logger.LogInformation("user :{User} try to negotiate hub: {Hub}", user, hub);
            if (string.IsNullOrEmpty(user))
            {
                return BadRequest("User ID is null or empty.");
            }
            if (!string.Equals(hub, _hubContexts.HubName, StringComparison.OrdinalIgnoreCase))
            {
                return NotFound($"Hub '{hub}' is not served by this server.");
            }

            var negotiation = await _hubContexts.PickRandom().NegotiateAsync(
                new NegotiationOptions { UserId = user }, HttpContext.RequestAborted);
            return new JsonResult(new Dictionary<string, string>
            {
                {"url", negotiation.Url},
                {"accessToken", negotiation.AccessToken}
            });
        }
    }
}