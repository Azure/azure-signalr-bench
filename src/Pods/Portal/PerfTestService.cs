#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Portal.Entity;

namespace Portal
{
    /// <summary>
    /// Test config and test run operations shared by the REST controllers and the MCP tools.
    /// </summary>
    public class PerfTestService
    {
        private readonly IPerfStorage _perfStorage;
        private readonly PerfState _perfState;
        private readonly ILogger<PerfTestService> _logger;

        public PerfTestService(IPerfStorage perfStorage, PerfState perfState, ILogger<PerfTestService> logger)
        {
            _perfStorage = perfStorage;
            _perfState = perfState;
            _logger = logger;
        }

        public async Task<List<TestConfigEntity>> ListConfigsAsync()
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var rows = await table.QueryAsync(FilterExpression.All()).ToListAsync();
            rows.Sort((a, b) => b.Timestamp!.Value.CompareTo(a.Timestamp!.Value));
            return rows;
        }

        public async Task<TestConfigEntity?> GetConfigAsync(string name)
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            return await table.GetAsync(name, name);
        }

        public async Task CreateConfigAsync(TestConfigEntity testConfigEntity, string? user)
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var exist = await table.GetAsync(testConfigEntity.PartitionKey, testConfigEntity.RowKey);
            if (exist != null)
            {
                throw new PerfRequestException($"Test name :{testConfigEntity.PartitionKey} already exist!");
            }

            testConfigEntity.User = user;
            InitConfig(testConfigEntity);
            await table.InsertAsync(testConfigEntity);
            _logger.LogInformation($"Create Test config:{JsonConvert.SerializeObject(testConfigEntity)}");
        }

        public async Task UpdateConfigAsync(TestConfigEntity testConfigEntity, string? user)
        {
            var table = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var exist = await table.GetAsync(testConfigEntity.PartitionKey, testConfigEntity.RowKey);
            if (exist == null)
            {
                throw new PerfRequestException($"Test name :{testConfigEntity.PartitionKey} don't exist!");
            }

            testConfigEntity.User = user;
            InitConfig(testConfigEntity);
            await table.UpdateAsync(testConfigEntity);
            _logger.LogInformation($"Update Test config:{JsonConvert.SerializeObject(testConfigEntity)}");
        }

        public async Task<TestStatusEntity> StartTestAsync(string testConfigEntityKey, string? user)
        {
            var configTable = await _perfStorage.GetTableAsync<TestConfigEntity>(PerfConstants.TableNames.TestConfig);
            var latestTestConfig = await configTable.GetAsync(testConfigEntityKey, testConfigEntityKey)
                                   ?? throw new PerfRequestException($"Test name :{testConfigEntityKey} doesn't exist!");
            latestTestConfig.InstanceIndex += 1;
            await configTable.UpdateAsync(latestTestConfig);
            var queueName = _perfState.GetQueueName(latestTestConfig.TargetLocation);
            var queue = await _perfStorage.GetQueueAsync<TestJob>(queueName);
            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var testEntity = new TestStatusEntity
            {
                User = user!,
                PartitionKey = latestTestConfig.PartitionKey,
                RowKey = latestTestConfig.InstanceIndex.ToString(),
                Status = "Init",
                Healthy = true,
                Report = "",
                ErrorInfo = "",
                Location = _perfState.GetLocation(latestTestConfig.TargetLocation),
                QueueName = queueName,
                Dir = latestTestConfig.Dir,
                DirectoryTestCount = 1,
                Config = JsonConvert.SerializeObject(latestTestConfig)
            };
            try
            {
                var exist = await statusTable.GetAsync(testEntity.PartitionKey, testEntity.RowKey);
                if (exist != null)
                {
                    await statusTable.DeleteAsync(exist);
                }

                await statusTable.InsertAsync(testEntity);
                await queue.SendAsync(latestTestConfig.ToTestJob(_perfState));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Start test error");
                throw;
            }

            return testEntity;
        }

        public async Task<List<TestStatusEntity>> StartBatchAsync(string[] directories, string index, string? user,
            int unitLimit = 300, int instanceLimit = 1, bool dryRun = false, bool replaceExisting = false)
        {
            if (directories.Length == 0 || directories.Any(string.IsNullOrWhiteSpace))
                throw new PerfRequestException("Specify at least one non-empty directory.");
            if (string.IsNullOrWhiteSpace(index) || unitLimit <= 0 || instanceLimit <= 0)
                throw new PerfRequestException("Specify a non-empty index and positive admission limits.");

            index = index.ToLowerInvariant();
            var requested = directories.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var configs = (await ListConfigsAsync()).Where(config => requested.Contains(config.Dir)).ToList();
            var missing = requested.Except(configs.Select(config => config.Dir), StringComparer.OrdinalIgnoreCase).ToArray();
            if (missing.Length > 0)
                throw new PerfRequestException($"Directories not found: {string.Join(", ", missing)}.");
            if (configs.Any(config => config.PartitionKey.Length + 2 + index.Length > 63))
                throw new PerfRequestException("Batch index is too long: test name plus '--' plus index must fit within 63 characters.");

            var statusTable = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var planned = new List<TestStatusEntity>();
            foreach (var config in configs)
            {
                if (!replaceExisting && await statusTable.GetAsync(config.PartitionKey, index) != null)
                    throw new PerfRequestException($"Run '{config.PartitionKey}--{index}' already exists. Use a new index to preserve the original evidence.");

                planned.Add(new TestStatusEntity
                {
                    User = user!,
                    PartitionKey = config.PartitionKey,
                    RowKey = index,
                    Status = dryRun ? "Planned (not queued)" : "Init",
                    Healthy = true,
                    Report = "",
                    ErrorInfo = "",
                    Location = _perfState.GetLocation(config.TargetLocation),
                    QueueName = _perfState.GetQueueName(config.TargetLocation),
                    Dir = config.Dir,
                    DirectoryTestCount = configs.Count(candidate => candidate.Dir == config.Dir),
                    Config = JsonConvert.SerializeObject(config)
                });
            }

            if (dryRun)
                return planned;

            var jobs = configs.Select(config => config.ToTestJob(_perfState, index, unitLimit, instanceLimit,
                config.Dir, configs.Count(candidate => candidate.Dir == config.Dir &&
                    _perfState.GetQueueName(candidate.TargetLocation) == _perfState.GetQueueName(config.TargetLocation)))).ToArray();
            _logger.LogInformation("Submitting batch {index}: {count} tests, directories {directories}, limits {unitLimit}/{instanceLimit}.",
                index, planned.Count, string.Join(", ", requested), unitLimit, instanceLimit);
            try
            {
                await Task.WhenAll(planned.Select(async (status, position) =>
                {
                    if (replaceExisting)
                    {
                        var existing = await statusTable.GetAsync(status.PartitionKey, status.RowKey);
                        if (existing != null)
                            await statusTable.DeleteAsync(existing);
                    }

                    await statusTable.InsertAsync(status);
                    var queue = await _perfStorage.GetQueueAsync<TestJob>(status.QueueName!);
                    await queue.SendAsync(jobs[position]);
                    _logger.LogInformation("Batch {index}: queued {testId} in {queue} at {queuedUtc}.",
                        index, status.TestId, status.QueueName, DateTimeOffset.UtcNow);
                }));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Batch {index} submission failed; some jobs may already be queued. Do not retry blindly.", index);
                throw new PerfRequestException(
                    $"Batch '{index}' submission failed; some jobs may already be queued. Inspect runs with this index before taking further action. Do not retry blindly.", e);
            }

            return planned;
        }

        public async Task<List<TestStatusEntity>> ListBatchStatusAsync(string index)
        {
            var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var rows = await table.QueryAsync(
                FilterExpression.Expression<TestStatusEntity>(run => run.RowKey == index)).ToListAsync();
            rows.Sort((a, b) => Nullable.Compare(b.Timestamp, a.Timestamp));
            rows.ForEach(row => _perfState.SetLinkPath(row));
            return rows;
        }

        /// <summary>
        /// Lists test runs of the last 7 days, or of one test (optionally one run index), plus all long running tests.
        /// </summary>
        public async Task<List<TestStatusEntity>> ListStatusAsync(string? key, string? index)
        {
            var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var dateTimeOffset = new DateTimeOffset(DateTime.UtcNow.AddDays(-7));
            var results = new List<TestStatusEntity>();

            if (string.IsNullOrEmpty(key))
            {
                results = await table
                    .QueryAsync(
                        FilterExpression.Expression<TestStatusEntity>(x => x.LastModified > dateTimeOffset))
                    .ToListAsync();
            }
            else if (string.IsNullOrEmpty(index))
            {
                results = await table.QueryAsync(
                    FilterExpression.KeyEqual(nameof(TestStatusEntity.PartitionKey), key)).ToListAsync();
            }
            else
            {
                var testStatus = await table.GetAsync(key, index);
                if (testStatus != null)
                {
                    results.Add(testStatus);
                }
            }

            // pull the long running test status
            var longruns = await table.QueryAsync(
                    FilterExpression.KeyEqual(nameof(TestStatusEntity.JobState), nameof(TestState.Longrun)))
                .ToListAsync();
            var hashSet = new HashSet<string?>(results.Select(entity => entity.TestId));
            results.AddRange(longruns.Where(entity => !hashSet.Contains(entity.TestId)));
            results.Sort((a, b) => b.Timestamp!.Value.CompareTo(a.Timestamp!.Value));
            results.ForEach(row => _perfState.SetLinkPath(row));
            return results;
        }

        public async Task<TestStatusEntity?> GetStatusAsync(string key, string index)
        {
            var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
            var testStatus = await table.GetAsync(key, index);
            if (testStatus != null)
            {
                _perfState.SetLinkPath(testStatus);
            }

            return testStatus;
        }

        public async Task CancelAsync(string testId, string queueName)
        {
            if (string.IsNullOrEmpty(queueName))
            {
                return;
            }

            var queue = await _perfStorage.GetQueueAsync<TestJob>(queueName, false);
            await queue.SendAsync(new TestJob
            {
                TestId = testId,
                Cancel = true
            });
        }

        private static void InitConfig(TestConfigEntity testConfigEntity)
        {
            try
            {
                testConfigEntity.Init();
            }
            catch (Exception e)
            {
                throw new PerfRequestException(e.Message);
            }
        }
    }

    /// <summary>
    /// An invalid request whose message is safe to return to the caller.
    /// </summary>
    public class PerfRequestException : Exception
    {
        public PerfRequestException(string message, Exception? innerException = null) : base(message, innerException)
        {
        }
    }
}
