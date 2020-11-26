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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("TestConfig")]
    [ApiController]
    public class TestConfigController : ControllerBase
    {
        private IPerfStorage _perfStorage;
      

        public TestConfigController(IPerfStorage perfStorage)
        {
            _perfStorage = perfStorage;
        }

        [HttpGet]
        public async Task<IEnumerable<TestConfigEntity>> Get()
        {

            var s = new Stopwatch();
            s.Start();
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            var rows = await table.QueryAsync(table.Rows
                                                   ).ToListAsync();
            rows.Sort((a,b)=>b.Timestamp.CompareTo(a.Timestamp));
            rows= rows.Select(r =>
            {
                r.ConnectionString = r.ConnectionString!=null?
                                     Regex.Replace(r.ConnectionString, "AccessKey=.*;V", "AccessKey=***;V"):null;
                return r;
            }).ToList();
            s.Stop();
            Console.WriteLine(s.ElapsedMilliseconds);
            return rows;
        }


        [HttpPut]
        public async Task CreateTestConfig(TestConfigEntity testConfigEntity)
        {
            testConfigEntity.PartitionKey = testConfigEntity.RowKey;
            testConfigEntity.Init();
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            await table.InsertAsync(testConfigEntity);
            Console.WriteLine(testConfigEntity.ToString());
        }

        [HttpPost("StartTest")]
        public async Task StartTestAsync(String testConfigEntityKey)
        {
            //increse counter first
            var configTable =await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig);
            var latestTestConfig =await configTable.GetFirstOrDefaultAsync(from row in configTable.Rows where row.PartitionKey == testConfigEntityKey select row);
            latestTestConfig.InstanceIndex += 1;
            await configTable.UpdateAsync(latestTestConfig);
            var queue= await _perfStorage.GetQueueAsync<TestJob>(Constants.QueueNames.PortalJob, true);
            await queue.SendAsync(latestTestConfig.ToTestJob(latestTestConfig.InstanceIndex));
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
            var testEntity = new TestStatusEntity()
            {
                PartitionKey = latestTestConfig.PartitionKey,
                RowKey = latestTestConfig.InstanceIndex.ToString(),
                Status = "Init",
                Healthy = true,
                Report = "",
                ErrorInfo = ""
            };
            try
            {
            await statusTable.InsertAsync(testEntity);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{key}")]
        public async Task Delete(string key)
        {
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(Constants.TableNames.TestConfig); 
            var config =await configTable.GetFirstOrDefaultAsync(from row in configTable.Rows where row.PartitionKey == key select row);
            await configTable.DeleteAsync(config);
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(Constants.TableNames.TestStatus);
            var statuses =
                await statusTable.QueryAsync(from row in statusTable.Rows where row.PartitionKey == key select row).ToListAsync();
            if(statuses.Count>0)
            await statusTable.BatchDeleteAsync(statuses);
        }

    }
}
