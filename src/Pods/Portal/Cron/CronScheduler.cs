using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Logging;
using NCrontab;
using Newtonsoft.Json;

namespace Portal.Cron
{
    public class CronScheduler : ICronScheduler
    {
        private IPerfStorage _perfStorage;
        private ILogger<CronScheduler> _logger;

        public CronScheduler(IPerfStorage perfStorage, ILogger<CronScheduler> logger)
        {
            _perfStorage = perfStorage;
            _logger = logger;
        }

        public void Start()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        await Task.Delay(40 * 1000);
                        var configTable =
                            await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
                        var configs =
                            await configTable.QueryAsync(from row in configTable.Rows
                                where row.Cron != "0"
                                select row).ToListAsync();
                        if (configs.Count == 0) continue;
                        var tasks = (from testConfigEntity in configs
                            let schedule = CrontabSchedule.Parse(testConfigEntity.Cron)
                            let nexTime = schedule.GetNextOccurrence(DateTime.Now)
                            let nexTimeStr = nexTime
                                .ToString(CultureInfo.InvariantCulture)
                            where DateTime.Now.AddSeconds(60) > nexTime && nexTimeStr != testConfigEntity.LastCronTime
                            select Task.Run(async () =>
                            {
                                try
                                {
                                    testConfigEntity.LastCronTime = nexTimeStr;
                                    testConfigEntity.InstanceIndex += 1;
                                    await configTable.UpdateAsync(testConfigEntity);
                                    var queue = await _perfStorage.GetQueueAsync<TestJob>(
                                        PerfConstants.QueueNames.PortalJob, true);
                                    var statusTable =
                                        await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames
                                            .TestStatus);
                                    var testEntity = new TestStatusEntity()
                                    {
                                        User = "cron",
                                        PartitionKey = testConfigEntity.PartitionKey,
                                        RowKey = testConfigEntity.InstanceIndex.ToString(),
                                        Status = "Init",
                                        Healthy = true,
                                        Report = "",
                                        ErrorInfo = "",
                                        Config = JsonConvert.SerializeObject(testConfigEntity)
                                    };
                                    await configTable.UpdateAsync(testConfigEntity);
                                    await statusTable.InsertAsync(testEntity);
                                    await queue.SendAsync(testConfigEntity.ToTestJob());
                                }
                                catch (Exception e)
                                {
                                    _logger.LogError(e, $"Cron test {testConfigEntity.PartitionKey} error");
                                }
                            })).ToList();
                        await Task.WhenAll(tasks);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "cron");
                    }
                }
            });
        }
    }
}