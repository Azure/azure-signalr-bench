using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Messages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Azure.SignalRBench.Coordinator
{
    public class TimeCoordinator
    {
        private readonly string _redisConnectionString;
        private readonly ILogger<TimeCoordinator> _logger;

        public TimeCoordinator(IConfiguration configuration, ILogger<TimeCoordinator> logger)
        {
            _redisConnectionString = configuration[PerfConstants.ConfigurationKeys.RedisConnectionStringKey];
            _logger = logger;
        }

        public async Task StartAsync()
        {
            var client = await MessageClient.ConnectAsync(_redisConnectionString, PerfConstants.Channel.All,
                PerfConstants.Roles.Contributor);
            while (true)
            {
                await client.BroadcastCoordinateTime();
                await Task.Delay(5000);
            }
        }
    }
}