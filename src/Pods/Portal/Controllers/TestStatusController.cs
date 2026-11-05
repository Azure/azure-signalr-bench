using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Globalization;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Azure.SignalRBench.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Portal.Controllers
{
    [Route("teststatus")]
    [ApiController]
    public class TestStatusController : ControllerBase
    {
        private readonly ILogger<TestStatusController> _logger;
        private readonly IPerfStorage _perfStorage;
        private readonly PerfTestService _perfTestService;

        public TestStatusController(IPerfStorage perfStorage, PerfTestService perfTestService,
            ILogger<TestStatusController> logger)
        {
            _perfStorage = perfStorage;
            _perfTestService = perfTestService;
            _logger = logger;
        }

        [HttpGet("list/{key?}/{index?}")]
        public async Task<IEnumerable<TestStatusEntity>> Get(string key, string index)
        {
            try
            {
                return await _perfTestService.ListStatusAsync(key, index);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }
        
        [HttpPost("cancel/{testId}/{queueName}")]
        public async Task Cancel(string testId, string queueName)
        {
            try
            {
                await _perfTestService.CancelAsync(testId, queueName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Cancel test status error");
                throw;
            }
        }

        [HttpGet("dir/list/{dir?}")]
        public async Task<IEnumerable<TestStatusEntity>> DirList(string dir, string index)
        {
            try
            {
                index = index.ToLower();
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                Expression<Func<TestStatusEntity, bool>> filterExpression = x => x.Dir == dir && x.RowKey ==index;

                var rows = await table.QueryAsync(
                    FilterExpression.Expression<TestStatusEntity>( x => x.Dir == dir && x.RowKey ==index)).ToListAsync();
                rows.Sort((a, b) =>
                    b.Timestamp!.Value.CompareTo(a.Timestamp!.Value)
                );
                return rows;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }

        [Authorize(Policy = PerfConstants.Policy.RoleLogin,
            Roles = PerfConstants.Roles.Contributor + "," + PerfConstants.Roles.Pipeline)]
        [HttpGet("dir/check/{dir}")]
        public async Task<TestResult> DirCheck(string dir, string index)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dir) || string.IsNullOrWhiteSpace(index))
                    return DirectoryFailure(dir, index, "A directory and run index are required.");

                index = index.ToLowerInvariant();
                var table = await _perfStorage.GetTableAsync<TestStatusEntity>(PerfConstants.TableNames.TestStatus);
                var rows = await table.QueryAsync(
                    FilterExpression.Expression<TestStatusEntity>( x => x.Dir == dir && x.RowKey ==index)).ToListAsync();
                if (rows.Count == 0)
                    return DirectoryFailure(dir, index, "No test runs were found.");

                var expectedCount = rows[0].DirectoryTestCount;
                var pending = false;
                foreach (var row in rows)
                {
                    if (!row.Healthy || !string.IsNullOrWhiteSpace(row.ErrorInfo) ||
                        row.Check == "Failed" || row.Check == "Cancelled")
                        return DirectoryFailure(dir, index, $"Test '{row.TestId}' failed or was cancelled.");
                    if (expectedCount <= 0 || row.DirectoryTestCount != expectedCount)
                        return DirectoryFailure(dir, index, "The original directory test count is missing or inconsistent.");
                    if (!Enum.TryParse<TestState>(row.JobState, out var state) || !Enum.IsDefined(typeof(TestState), state))
                        return DirectoryFailure(dir, index, $"Test '{row.TestId}' has an unknown lifecycle state.");
                    // Suspicious may clear as later rounds succeed; it is not final while the run is active.
                    if (!string.IsNullOrWhiteSpace(row.Check) &&
                        !(row.Check == "Suspicious" && (state == TestState.InProgress || state == TestState.Longrun)))
                        return DirectoryFailure(dir, index, $"Test '{row.TestId}' did not pass its business checks.");

                    if (state == TestState.InProgress || state == TestState.Cleaning || state == TestState.Longrun)
                    {
                        pending = true;
                        continue;
                    }

                    if (state != TestState.Cleaned || row.LongRun)
                        return DirectoryFailure(dir, index, $"Test '{row.TestId}' is not a completed finite run.");
                    if (!HasCompleteReport(row))
                        return DirectoryFailure(dir, index, $"Test '{row.TestId}' has no complete report matching its config snapshot.");
                }

                if (rows.Count > expectedCount)
                    return DirectoryFailure(dir, index, "More test runs were found than the directory snapshot expects.");
                if (pending)
                    return new TestResult { Status = "wait" };
                if (rows.Count != expectedCount)
                    return DirectoryFailure(dir, index, "Some runs from the original directory submission are missing.");
                return new TestResult { Status = "succeed" };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get test status error");
                throw;
            }
        }

        private TestResult DirectoryFailure(string dir, string index, string reason)
        {
            _logger.LogWarning("Directory {dir}, run {index}: {reason}", dir, index, reason);
            return new TestResult { Status = "fail" };
        }

        private bool HasCompleteReport(TestStatusEntity row)
        {
            if (string.IsNullOrWhiteSpace(row.Config) || string.IsNullOrWhiteSpace(row.Report))
                return false;
            try
            {
                var settings = new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error };
                var config = JObject.Parse(row.Config, settings);
                var roundCount = config["RoundNum"];
                if (roundCount?.Type != JTokenType.Integer ||
                    !int.TryParse(roundCount.ToString(Formatting.None), NumberStyles.None, CultureInfo.InvariantCulture,
                        out var expectedRounds) || expectedRounds <= 0)
                    return false;

                var report = JArray.Parse(row.Report, settings);
                if (report.Count != expectedRounds || report.Any(round => round is not JObject))
                    return false;
                var requiredCounters = new[]
                {
                    nameof(RoundStatus.ActiveConnection), nameof(RoundStatus.RoundConnected),
                    nameof(RoundStatus.ConnectedCount), nameof(RoundStatus.ReconnectingCount),
                    nameof(RoundStatus.TotalReconnectCount), nameof(RoundStatus.MessageSent),
                    nameof(RoundStatus.MessageRecieved), nameof(RoundStatus.ExpectedRecievedMessageCount)
                };
                foreach (var round in report.Cast<JObject>())
                {
                    if (requiredCounters.Any(field => round[field]?.Type != JTokenType.Integer) ||
                        round[nameof(RoundStatus.Time)] == null ||
                        round[nameof(RoundStatus.Latency)] is not JObject latency ||
                        Enum.GetNames(typeof(LatencyClass)).Any(bucket => latency[bucket]?.Type != JTokenType.Integer))
                        return false;
                }
                var rounds = report.ToObject<RoundStatus[]>();
                return rounds != null && rounds.All(round => round != null && round.RoundConnected > 0 &&
                    round.ActiveConnection >= 0 && round.ConnectedCount >= 0 && round.ReconnectingCount >= 0 &&
                    round.TotalReconnectCount >= 0 && round.MessageSent >= 0 && round.MessageRecieved >= 0 &&
                    round.ExpectedRecievedMessageCount >= 0 && round.Time != default &&
                    round.Latency != null && round.Latency.Values.All(value => value >= 0));
            }
            catch (JsonException)
            {
                _logger.LogWarning("Test {testId} has an invalid config snapshot or round report.", row.TestId);
                return false;
            }
        }
    }

    public class TestResult
    {
        public string Status { get; set; }
    }
}