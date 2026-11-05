#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.SignalRBench.Common;
using Azure.SignalRBench.Coordinator.Entities;
using Microsoft.AspNetCore.Authorization;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Portal.Auth;
using Portal.Entity;

namespace Portal.Mcp
{
    /// <summary>
    /// MCP tools exposing the perf portal to agents. Every call runs as the signed-in Entra user:
    /// the endpoint requires the Reader or Contributor app role, and tools that change state require Contributor.
    /// </summary>
    [McpServerToolType]
    public sealed class PerfMcpTools
    {
        private const int MaxRuns = 200;

        // Service access keys must never reach the model context, whatever the caller's role.
        private static readonly Regex AccessKeyRegex = new Regex(@"(AccessKey=)[^;""\\\s]+",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly HashSet<string> ProtectedConfigFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            nameof(TestConfigEntity.PartitionKey),
            nameof(TestConfigEntity.RowKey),
            nameof(TestConfigEntity.User),
            nameof(TestConfigEntity.InstanceIndex),
            nameof(TestConfigEntity.LongRunIndex),
            nameof(TestConfigEntity.LastCronTime),
            nameof(TestConfigEntity.Timestamp),
            nameof(TestConfigEntity.LastModified),
            nameof(TestConfigEntity.ETag),
        };

        private static readonly Dictionary<string, PropertyInfo> SettableConfigFields = typeof(TestConfigEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite && !ProtectedConfigFields.Contains(p.Name))
            .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

        private readonly PerfTestService _perfTestService;
        private readonly PerfState _perfState;

        public PerfMcpTools(PerfTestService perfTestService, PerfState perfState)
        {
            _perfTestService = perfTestService;
            _perfState = perfState;
        }

        [McpServerTool(Name = "get_perf_info", Title = "Get perf portal info", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns the signed-in user, their perf roles, the portal location and the host locations that can be used as a test config's TargetLocation.")]
        public string GetPerfInfo(RequestContext<CallToolRequestParams> context)
        {
            var user = context.User!;
            return Serialize(new
            {
                User = AuthUtil.GetUserName(user),
                Roles = new[] { PerfConstants.Roles.Contributor, PerfConstants.Roles.Reader }.Where(user.IsInRole),
                _perfState.Location,
                _perfState.DefaultHostLocation,
                _perfState.HostLocations,
                _perfState.PPEEnabled,
            });
        }

        [McpServerTool(Name = "list_test_configs", Title = "List test configs", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists perf test configs (summary fields only), newest first. Use get_test_config for all fields of one config.")]
        public async Task<string> ListTestConfigs(
            [Description("Only return configs in this directory (case-insensitive).")] string? dir = null,
            [Description("Only return configs whose name contains this text (case-insensitive).")] string? nameContains = null)
        {
            var configs = await _perfTestService.ListConfigsAsync();
            return Serialize(configs
                .Where(c => dir == null || string.Equals(c.Dir, dir, StringComparison.OrdinalIgnoreCase))
                .Where(c => nameContains == null || c.PartitionKey.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                .Select(c => new
                {
                    Name = c.PartitionKey,
                    c.Dir,
                    c.Service,
                    c.Mode,
                    c.Framework,
                    c.Scenario,
                    c.Protocol,
                    c.ClientCons,
                    c.SignalRUnitSize,
                    c.TargetLocation,
                    c.Cron,
                    c.Tags,
                    c.User,
                    LastRunIndex = c.InstanceIndex,
                    c.Timestamp,
                }));
        }

        [McpServerTool(Name = "get_test_config", Title = "Get test config", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns all fields of a perf test config. Service access keys are masked.")]
        public async Task<string> GetTestConfig([Description("Test config name.")] string name)
        {
            var config = await _perfTestService.GetConfigAsync(name) ?? throw NotFound(name);
            return Serialize(ToJson(config));
        }

        [Authorize(Roles = PerfConstants.Roles.Contributor)]
        [McpServerTool(Name = "create_test_config", Title = "Create test config", Destructive = false, OpenWorld = false)]
        [Description("Creates a perf test config. Starts from the defaults, or from a copy of an existing config (including its stored connection string) when copyFrom is set; the copy's cron schedule is disabled. Then applies settings. Requires the Contributor role.")]
        public async Task<string> CreateTestConfig(
            RequestContext<CallToolRequestParams> context,
            [Description("Name of the new test config.")] string name,
            [Description("Fields to set, using the field names returned by get_test_config, e.g. {\"ClientCons\": 2000, \"SignalRUnitSize\": 1, \"Scenario\": \"Echo\"}.")] Dictionary<string, JsonElement>? settings = null,
            [Description("Name of an existing test config to copy.")] string? copyFrom = null)
        {
            ValidateName(name);
            TestConfigEntity config;
            if (copyFrom == null)
            {
                config = new TestConfigEntity();
            }
            else
            {
                config = await _perfTestService.GetConfigAsync(copyFrom) ?? throw NotFound(copyFrom);
                config.InstanceIndex = 0;
                config.LongRunIndex = 0;
                config.Cron = "0";
                config.LastCronTime = "0";
                config.Timestamp = null;
            }

            config = ApplySettings(config, settings);
            config.PartitionKey = name;
            config.RowKey = name;
            await RunAsync(() => _perfTestService.CreateConfigAsync(config, AuthUtil.GetUserName(context.User!)));
            return Serialize(ToJson(config));
        }

        [Authorize(Roles = PerfConstants.Roles.Contributor)]
        [McpServerTool(Name = "update_test_config", Title = "Update test config", Destructive = true, Idempotent = true, OpenWorld = false)]
        [Description("Changes fields of an existing perf test config; fields not in settings keep their current values. Requires the Contributor role.")]
        public async Task<string> UpdateTestConfig(
            RequestContext<CallToolRequestParams> context,
            [Description("Test config name.")] string name,
            [Description("Fields to change, using the field names returned by get_test_config.")] Dictionary<string, JsonElement> settings)
        {
            var existing = await _perfTestService.GetConfigAsync(name) ?? throw NotFound(name);
            var config = ApplySettings(existing, settings);
            config.ETag = existing.ETag;
            await RunAsync(() => _perfTestService.UpdateConfigAsync(config, AuthUtil.GetUserName(context.User!)));
            return Serialize(ToJson(config));
        }

        [Authorize(Roles = PerfConstants.Roles.Contributor)]
        [McpServerTool(Name = "start_test", Title = "Start test", Destructive = false, OpenWorld = true)]
        [Description("Starts a new run of a perf test config. The run provisions test resources and can take a long time; poll get_test_run for progress. Requires the Contributor role.")]
        public async Task<string> StartTest(
            RequestContext<CallToolRequestParams> context,
            [Description("Test config name.")] string name)
        {
            var run = await RunAsync(() => _perfTestService.StartTestAsync(name, AuthUtil.GetUserName(context.User!)));
            return Serialize(Summarize(run));
        }

        [Authorize(Roles = PerfConstants.Roles.Contributor)]
        [McpServerTool(Name = "start_test_batch", Title = "Start pipeline-style test batch", Destructive = true, OpenWorld = false)]
        [Description("Submits all configs in the specified directories concurrently through the pipeline batch path. Preserves directory admission, per-directory totals and one shared index; does not synchronize Pod readiness. Defaults to dry-run (no writes). Use a new index for every attempt; existing runs are never overwritten. Submission is not transactional: on error inspect that index, never retry blindly. Requires Contributor.")]
        public async Task<string> StartTestBatch(
            RequestContext<CallToolRequestParams> context,
            [Description("Exact test directory names to submit together.")] string[] directories,
            [Description("Unique batch index: lowercase letters, digits and single hyphens; keep test name plus '--' plus index within 63 characters.")] string index,
            [Description("Per-directory service unit admission limit (pipeline uses 300).")] int unitLimit = 300,
            [Description("Per-directory concurrent instance limit (pipeline uses 1).")] int instanceLimit = 1,
            [Description("When true, only return the plan without storing or queuing anything. Pass false to start.")] bool dryRun = true)
        {
            if (!Regex.IsMatch(index, "^[a-z0-9]+(-[a-z0-9]+)*$"))
                throw new McpException("Invalid batch index. Use lowercase letters, digits and single hyphens.");
            var submittedUtc = DateTimeOffset.UtcNow;
            var runs = await RunAsync(() => _perfTestService.StartBatchAsync(directories, index,
                AuthUtil.GetUserName(context.User!), unitLimit, instanceLimit, dryRun));
            return Serialize(new
            {
                Index = index,
                DryRun = dryRun,
                UnitLimit = unitLimit,
                InstanceLimit = instanceLimit,
                SubmittedUtc = submittedUtc,
                CompletedUtc = DateTimeOffset.UtcNow,
                Runs = runs.Select(Summarize)
            });
        }

        [McpServerTool(Name = "list_test_runs", Title = "List test runs", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Lists test runs (summary fields only), newest first. With index, finds that exact batch across all history (optionally filtered by name), without unrelated long runs. Otherwise lists one config's history or the last 7 days plus long runs. Use get_test_run for reports. A full maxCount response may be truncated.")]
        public async Task<string> ListTestRuns(
            [Description("Test config name.")] string? name = null,
            [Description("Maximum number of runs to return (default 50, at most 200).")] int maxCount = 50,
            [Description("Exact historical batch/run index, e.g. 20260921-1 or release-2026-09-v6-retry-1.")] string? index = null)
        {
            var runs = index == null
                ? await _perfTestService.ListStatusAsync(name, null)
                : await _perfTestService.ListBatchStatusAsync(index);
            return Serialize(runs
                .Where(run => index == null || name == null || string.Equals(run.PartitionKey, name, StringComparison.Ordinal))
                .Take(Math.Clamp(maxCount, 1, MaxRuns)).Select(Summarize));
        }

        [McpServerTool(Name = "get_test_run", Title = "Get test run", ReadOnly = true, Idempotent = true, OpenWorld = false)]
        [Description("Returns one test run including its status, error info, report and the config it ran with. Service access keys are masked.")]
        public async Task<string> GetTestRun(
            [Description("Test config name.")] string name,
            [Description("Run index, as returned by start_test or list_test_runs.")] string index)
        {
            var run = await _perfTestService.GetStatusAsync(name, index) ?? throw RunNotFound(name, index);
            return Serialize(new
            {
                run.TestId,
                Name = run.PartitionKey,
                Index = run.RowKey,
                run.Status,
                run.JobState,
                run.Healthy,
                run.ErrorInfo,
                run.Check,
                run.User,
                run.Location,
                run.Dir,
                run.DirectoryTestCount,
                run.LongRun,
                run.GrafanaPath,
                run.LastModified,
                run.Report,
                run.Config,
            });
        }

        [Authorize(Roles = PerfConstants.Roles.Contributor)]
        [McpServerTool(Name = "cancel_test", Title = "Cancel test run", Destructive = true, Idempotent = true, OpenWorld = true)]
        [Description("Requests cancellation of a test run. Requires the Contributor role.")]
        public async Task<string> CancelTest(
            [Description("Test config name.")] string name,
            [Description("Run index, as returned by start_test or list_test_runs.")] string index)
        {
            var run = await _perfTestService.GetStatusAsync(name, index) ?? throw RunNotFound(name, index);
            if (string.IsNullOrEmpty(run.QueueName))
            {
                throw new McpException($"Test run {run.TestId} has no job queue and cannot be cancelled.");
            }

            await _perfTestService.CancelAsync(run.TestId!, run.QueueName);
            return Serialize(new { run.TestId, CancelRequested = true });
        }

        private static object Summarize(TestStatusEntity run) => new
        {
            run.TestId,
            Name = run.PartitionKey,
            Index = run.RowKey,
            run.Status,
            run.JobState,
            run.Healthy,
            run.ErrorInfo,
            run.Check,
            run.User,
            run.Location,
            run.Dir,
            run.DirectoryTestCount,
            run.LongRun,
            run.LastModified,
        };

        private static TestConfigEntity ApplySettings(TestConfigEntity config, Dictionary<string, JsonElement>? settings)
        {
            var json = ToJson(config);
            foreach (var (key, value) in settings ?? new Dictionary<string, JsonElement>())
            {
                if (!SettableConfigFields.TryGetValue(key, out var property))
                {
                    throw new McpException(
                        $"'{key}' is not a settable test config field. Settable fields: {string.Join(", ", SettableConfigFields.Keys.OrderBy(k => k))}.");
                }

                json[property.Name] = JToken.Parse(value.GetRawText());
            }

            try
            {
                return json.ToObject<TestConfigEntity>()!;
            }
            catch (Exception e) when (e is Newtonsoft.Json.JsonException || e is ArgumentException || e is FormatException)
            {
                throw new McpException($"Invalid settings: {e.Message}");
            }
        }

        private static JObject ToJson(TestConfigEntity config)
        {
            var json = JObject.FromObject(config);
            json.Remove(nameof(TestConfigEntity.ETag));
            return json;
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(new[] { '/', '\\', '#', '?' }) >= 0 ||
                name.Any(char.IsControl))
            {
                throw new McpException($"Invalid test config name '{name}'.");
            }
        }

        private static async Task RunAsync(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (PerfRequestException e)
            {
                throw new McpException(e.Message);
            }
        }

        private static async Task<T> RunAsync<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (PerfRequestException e)
            {
                throw new McpException(e.Message);
            }
        }

        private static McpException NotFound(string name) => new McpException($"Test config '{name}' doesn't exist.");

        private static McpException RunNotFound(string name, string index) =>
            new McpException($"Test run '{name}' index '{index}' doesn't exist.");

        private static string Serialize(object value) =>
            AccessKeyRegex.Replace(JsonConvert.SerializeObject(value), "$1***");
    }
}
