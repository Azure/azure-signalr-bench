using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Portal.Entities;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("TestConfig")]
    [ApiController]
    public class TestConfigController : ControllerBase
    {
        private PerfStorage _perfStorage;

        public TestConfigController(PerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }

        [HttpGet]
        public async Task<IEnumerable<TestConfigEntity>> Get()
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            var rows = await table.QueryAsync(table.Rows
                                                   ).ToListAsync();
            return rows;
        }


        [HttpPut]
        public async Task CreateTestConfig(TestConfigEntity testConfigEntity)
        {
            testConfigEntity.PartitionKey = testConfigEntity.RowKey;
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            await table.InsertAsync(testConfigEntity);
            Console.WriteLine(testConfigEntity.ToString());
        }

        [HttpPost("StartTest")]
        public async Task StartTestAsync(TestConfigEntity testConfigEntity)
        {
            //increse counter first
            var configTable =await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            var latestTestConfig =await configTable.GetFirstOrDefaultAsync(from row in configTable.Rows where row.PartitionKey == testConfigEntity.PartitionKey select row);
            latestTestConfig.Index += 1;
            await configTable.UpdateAsync(latestTestConfig);
            var queue= await _perfStorage.GetQueueAsync<TestJob>(Constants.QueueNames.PortalJob, true);
            await queue.SendAsync(testConfigEntity.ToTestJob(latestTestConfig.Index));
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
            var testEntity = new TestStatusEntity()
            {
                PartitionKey = latestTestConfig.PartitionKey,
                RowKey = latestTestConfig.Index.ToString(),
                Status = "Init"
            };
            await statusTable.InsertAsync(testEntity);
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }

    }
}
