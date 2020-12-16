using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SignalRUpstream.Controllers
{
    [Route("upstream")]
    [ApiController]
    public class UpStreamController: ControllerBase
    {
        private ILogger<UpStreamController> _logger;
        private MessagePublisher _messagePublisher;
        public UpStreamController(ILogger<UpStreamController> logger,MessagePublisher messagePublisher)
        {
            _logger = logger;
            _messagePublisher = messagePublisher;
        }
        
        [HttpPost("{hub}/negotiate")]
        public async Task OnConnectedAsync()
        {
        }
        
        public  async Task OnDisconnectedAsync(Exception exception)
        {
        }

        public void Echo(long ticks, string payload)
        {
        }
        
        public void SendToConnection(string connectionId,long ticks, string payload)
        {
        }

        public void Broadcast(long ticks, string payload)
        {
        }

        public async Task JoinGroups(string group)
        {
        }

        public async Task LeaveGroups(string group)
        {
        }

        public void GroupBroadcast(string group, long ticks, string payload)
        {
        }
    }
}