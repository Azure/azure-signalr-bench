using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;
using Portal.Entity;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("TestConfig")]
    [ApiController]
    public class TestConfigController : ControllerBase
    {
        private readonly PerfState _perfState;
        private readonly ILogger<TestConfigController> _logger;
        private readonly IPerfStorage _perfStorage;
        private readonly PerfTestService _perfTestService;

        public TestConfigController(IPerfStorage perfStorage, PerfState perfState,
            PerfTestService perfTestService, ILogger<TestConfigController> logger)
        {
            _perfStorage = perfStorage;
            _perfState = perfState;
            _perfTestService = perfTestService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<TestConfigEntity>> Get()
        {
            return await _perfTestService.ListConfigsAsync();
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPut]
        public async Task<ActionResult> CreateTestConfig(TestConfigEntity testConfigEntity)
        {
            try
            {
                await _perfTestService.CreateConfigAsync(testConfigEntity, User.Identity.Name);
            }
            catch (PerfRequestException e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }
        
        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPatch]
        public async Task<ActionResult> PatchTestConfig(TestConfigEntity testConfigEntity)
        {
            try
            {
                await _perfTestService.UpdateConfigAsync(testConfigEntity, User.Identity.Name);
            }
            catch (PerfRequestException e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor + "," + PerfConstants.Roles.Pipeline)]
        [HttpPost("StartTest/{testConfigEntityKey}")]
        public async Task<ActionResult> StartTestAsync(string testConfigEntityKey)
        {
            try
            {
                await _perfTestService.StartTestAsync(testConfigEntityKey, User.Identity.Name);
            }
            catch (PerfRequestException e)
            {
                return BadRequest(e.Message);
            }

            return Ok();
        }
        
        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPost("StartLongRunTest/{testConfigEntityKey}")]
        public async Task StartLongRunTestAsync(string testConfigEntityKey)
        {
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            
            var latestTestConfig = await configTable.GetAsync(testConfigEntityKey,testConfigEntityKey);
            
            latestTestConfig.LongRunIndex += 1;
            await configTable.UpdateAsync(latestTestConfig);
            var queueName = _perfState.GetQueueName(latestTestConfig.TargetLocation);
            var queue = await _perfStorage.GetQueueAsync<TestJob>(queueName);
            var testEntity = new TestStatusEntity
            {
                User = User.Identity.Name,
                PartitionKey = latestTestConfig.PartitionKey,
                RowKey = $"longrun-{latestTestConfig.LongRunIndex.ToString()}",
                Status = "Init",
                Healthy = true,
                Report = "",
                ErrorInfo = "",
                LongRun = true,
                Dir = latestTestConfig.Dir,
                DirectoryTestCount = 1,
                Location = _perfState.GetLocation(latestTestConfig.TargetLocation),
                QueueName = queueName,
                Config = JsonConvert.SerializeObject(latestTestConfig)
            };
            try
            {
               
                await statusTable.InsertAsync(testEntity);
                await queue.SendAsync(latestTestConfig.ToTestJob(_perfState,testEntity.RowKey ));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Start long run test error");
                throw;
            }
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpDelete("{key}")]
        public async Task Delete(string key)
        {
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var config =
                await configTable.GetAsync(key, key);
            await configTable.DeleteAsync(config);
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPut("move/{type}/{source}/{target}")]
        public async Task<ActionResult> Move(string type, string source, string target)
        {
            switch (type)
            {
                case "jobConfig":
                {
                    var configTable =
                        await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
                    var config =
                        await configTable.GetAsync(source, source);
                    if (config == null) return BadRequest($"testName {source} doesn't exist");
                    config.Dir = target;
                    await configTable.UpdateAsync(config);
                    return Ok();
                }
                case "dir":
                {
                    var configTable =
                        await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
                    var filter = FilterExpression.KeyEqual(nameof(TestConfigEntity.Dir), source);
                    var configs =
                        await configTable.QueryAsync(filter).ToListAsync();
                    if (configs.Count == 0) return BadRequest($"dir {source} doesn't exist");
                    var tasks = new List<Task>();
                    foreach (var testConfigEntity in configs)
                    {
                        testConfigEntity.Dir = target;
                        tasks.Add(Task.Run(async () => await configTable.UpdateAsync(testConfigEntity)));
                    }

                    await Task.WhenAll(tasks);
                    return Ok();
                }
                default:
                    return BadRequest("unsupported");
            }
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPut("rename/{source}/{target}")]
        public async Task<ActionResult> Rename(string source, string target)
        {
            var configTable =
                await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var oldOne =
                await configTable.GetAsync(source, source);
            if (oldOne == null) return BadRequest($"testName {source} doesn't exist");
            var newOne = await configTable.GetAsync(target, target);
            if (newOne != null) return BadRequest($"testName {target} already exist");
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var newTestStatusEntities = await statusTable.QueryAsync(
                FilterExpression.KeyEqual(nameof(TestStatusEntity.PartitionKey), target)).ToListAsync();
            if (newTestStatusEntities.Count != 0) return BadRequest($"teststatus for {target} already exist");
            //create new config
            oldOne.PartitionKey = target;
            oldOne.RowKey = target;
            await configTable.InsertAsync(oldOne);
            var oldTestStatusEntities = await statusTable.QueryAsync(
                FilterExpression.KeyEqual(nameof(TestStatusEntity.PartitionKey), source)).ToListAsync();
            //create new teststatus
            foreach (var status in oldTestStatusEntities)
            {
                status.PartitionKey = target;
                await statusTable.InsertAsync(status);
            }

            //delete testconfig
            await Delete(source);
            return Ok();
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPut("cron/{key}")]
        public async Task<ActionResult> Cron(string key, string cron)
        {
            //validate cron
            if (cron != "0")
            {
                cron = cron.Replace("_", " ");
                try
                {
                    CrontabSchedule.Parse(cron);
                }
                catch (Exception)
                {
                    return BadRequest($"invalid cron expression: {cron} ");
                }
            }

            var configTable =
                await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var config =
                await configTable.GetAsync(key, key);
            if (config == null) return BadRequest($"testName {key} doesn't exist");
            config.Cron = cron;
            await configTable.UpdateAsync(config);
            return Ok();
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor)]
        [HttpPut("batch/{testName}")]
        public async Task<ActionResult> Batch(string testName, string dir, string units)
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var testConfig = await table.GetAsync(testName, testName);
            if (testConfig == null)
            {
                return BadRequest($"Test name :{testName} doesn't exist!");
            }

            try
            {
                var configs = testConfig.GenerateTestConfigs(dir, units);

                var tasks = new List<Task>();
                foreach (var testConfigEntity in configs)
                {
                    testConfigEntity.User = User.Identity.Name;
                    tasks.Add(Task.Run(async () =>
                    {
                        var exist = await table.GetAsync(testConfigEntity.PartitionKey, testConfigEntity.RowKey);
                        if (exist != null)
                        {
                            await Delete(exist.PartitionKey);
                        }

                        await table.InsertAsync(testConfigEntity);
                    }));
                }

                await Task.WhenAll(tasks);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor + "," + PerfConstants.Roles.Pipeline)]
        [HttpPost("batch/StartTest/{dir}")]
        public async Task<ActionResult> StartTestAsync(string dir, string index, int unitLimit = 300,
            int instanceLimit = 10)
        {
            try
            {
                await _perfTestService.StartBatchAsync(new[] { dir }, index, User.Identity.Name,
                    unitLimit, instanceLimit, replaceExisting: true);
                return Ok();
            }
            catch (PerfRequestException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}