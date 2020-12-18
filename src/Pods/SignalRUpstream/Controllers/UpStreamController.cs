using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SignalRUpstream.Entities;

namespace SignalRUpstream.Controllers
{
    [Route("upstream")]
    [ApiController]
    public class UpStreamController : ControllerBase
    {
        private ILogger<UpStreamController> _logger;
        private MessagePublisher _messagePublisher;

        public UpStreamController(ILogger<UpStreamController> logger, MessagePublisher messagePublisher)
        {
            _logger = logger;
            _messagePublisher = messagePublisher;
        }

        [HttpPost("{hub}/api/connections/connected")]
        public async Task OnConnectedAsync()
        {
            _logger.LogInformation("Connected.");
        }

        [HttpPost("{hub}/api/connections/disconnected")]
        public async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("disConnected.");
        }

        [HttpPost("{hub}/api/messages/{method}")]
        public async Task Echo(string method)
        {
            using (var sr = new StreamReader(Request.Body))
            {
                var body = await sr.ReadToEndAsync();
                var user = Request.Headers["X-ASRS-User-Id"];
                var upstreamBody = JsonConvert.DeserializeObject<UpstreamBody>(body);
                _logger.LogInformation($"method:{method}, body:{upstreamBody}");
                switch (upstreamBody.Target)
                {
                    case "add":
                    case "remove":
                        await _messagePublisher.ManageUserGroupAsync(upstreamBody.Target, user,
                            (string) upstreamBody.Arguments[0]);
                        break;
                    default:
                        await _messagePublisher.SendMessagesAsync(upstreamBody.Target,
                            (string) upstreamBody.Arguments[0], (long) upstreamBody.Arguments[1],
                            (string) upstreamBody.Arguments[2]);
                        break;
                }
            }
        }
    }
}