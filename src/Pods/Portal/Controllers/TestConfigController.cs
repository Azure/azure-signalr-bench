using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("TestConfig")]
    [ApiController]
    public class TestConfigController : ControllerBase
    {
        private IPerfStorage _perfStorage;
        private ILogger<TestConfigController> _logger;

        public TestConfigController(IPerfStorage perfStorage, ILogger<TestConfigController> logger)
        {
            _perfStorage = perfStorage;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IEnumerable<TestConfigEntity>> Get()
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var rows = await table.QueryAsync(table.Rows
            ).ToListAsync();
            rows.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            // rows = rows.Select(r =>
            // {
            //     r.ConnectionString = r.ConnectionString != null
            //         ? Regex.Replace(r.ConnectionString, "AccessKey=.*;V", "AccessKey=***;V")
            //         : null;
            //     return r;
            // }).ToList();
            return rows;
        }


        [HttpPut]
        public async Task CreateTestConfig(TestConfigEntity testConfigEntity)
        {
            testConfigEntity.User = User.Identity.Name;
            testConfigEntity.PartitionKey = testConfigEntity.RowKey;
            testConfigEntity.Init();
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            await table.InsertAsync(testConfigEntity);
            _logger.LogInformation($"Create Test config:{JsonConvert.SerializeObject(testConfigEntity)}");
        }

        [HttpPost("StartTest/{testConfigEntityKey}")]
        public async Task StartTestAsync(String testConfigEntityKey)
        {
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var latestTestConfig = await configTable.GetFirstOrDefaultAsync(from row in configTable.Rows
                                                                            where row.PartitionKey == testConfigEntityKey
                                                                            select row);
            latestTestConfig.InstanceIndex += 1;
            await configTable.UpdateAsync(latestTestConfig);
            var queue = await _perfStorage.GetQueueAsync<TestJob>(PerfConstants.QueueNames.PortalJob, true);
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var testEntity = new TestStatusEntity()
            {
                User = User.Identity.Name,
                PartitionKey = latestTestConfig.PartitionKey,
                RowKey = latestTestConfig.InstanceIndex.ToString(),
                Status = "Init",
                Healthy = true,
                Report = "",
                ErrorInfo = "",
                Config = JsonConvert.SerializeObject(latestTestConfig)
            };
            try
            {
                await statusTable.InsertAsync(testEntity);
                await queue.SendAsync(latestTestConfig.ToTestJob(latestTestConfig.InstanceIndex));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Start test error");
                throw;
            }
        }

        [HttpDelete("{key}")]
        public async Task Delete(string key)
        {
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var config =
                await configTable.GetFirstOrDefaultAsync(from row in configTable.Rows
                                                         where row.PartitionKey == key
                                                         select row);
            await configTable.DeleteAsync(config);
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var statuses =
                await statusTable.QueryAsync(from row in statusTable.Rows where row.PartitionKey == key select row)
                    .ToListAsync();
            if (statuses.Count > 0)
                await statusTable.BatchDeleteAsync(statuses);
        }
    }
}