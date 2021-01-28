using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Security.KeyVault.Secrets;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Portal.Entity;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("Home")]
    [ApiController]
    public class HomeController : ControllerBase
    {
        private readonly ClusterState _clusterState;
        private ILogger<HomeController> _logger;
        private IPerfStorage _perfStorage;
        private SecretClient _secretClient;


        public HomeController(IPerfStorage perfStorage, SecretClient secretClient, ILogger<HomeController> logger,
            ClusterState clusterState)
        {
            _perfStorage = perfStorage;
            _secretClient = secretClient;
            _clusterState = clusterState;
            _logger = logger;
        }

        [HttpGet("info")]
        public async Task<BasicInfo> Get()
        {
            var basicInfo = new BasicInfo
            {
                User = User.Identity.Name,
                Location = _clusterState.Location,
                PPEEnabled = _clusterState.PPEEnabled
            };
            return basicInfo;
        }
        
        [HttpPut("auth/{userName}")]
        public async Task<ActionResult> Auth(string userName, string role)
        {
            var password = Guid.NewGuid().ToString(); 
            var userIdentity=new UserIdentity()
            {
                PartitionKey = userName,
                RowKey = userName,
                Role = role,
                Password = password
            };
            var table = await _perfStorage.GetTableAsync<UserIdentity>(PerfConstants.TableNames.UserIdentity);
            var user = await table.GetFirstOrDefaultAsync(from row in table.Rows
                where row.PartitionKey == userName
                select row);
            if (user != null)
            {
                user.Role = role;
                user.Password = password;
                await table.UpdateAsync(user);
            }
            else
            {
                await table.InsertAsync(userIdentity);
            }
            _logger.LogInformation($"Auth user :{userName}");
            return Ok($"Password:  {password}");
        }
    }
}