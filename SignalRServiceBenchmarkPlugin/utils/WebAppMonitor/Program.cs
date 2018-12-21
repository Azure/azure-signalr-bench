using System;
using CommandLine;

namespace azuremonitor
{
    public class Program
    {
        private static ArgsOption ParseArgs(string[] args)
        {
            var argsOption = new ArgsOption();
            var result = Parser.Default.ParseArguments<ArgsOption>(args)
                .WithParsed(options => argsOption = options)
                .WithNotParsed(error =>
                {
                    Console.WriteLine($"Error in parsing arguments: {error}");
                    throw new ArgumentException($"Error in parsing arguments: {error}");
                });
            return argsOption;
        }

        public static void Main(string[] args)
        {
            var argsOption = ParseArgs(args);
            var azureMonitor = new AzureMonitor(argsOption);
            azureMonitor.Login();
            azureMonitor.ListMetricDefinitionsSample();
            azureMonitor.QueryMetricDefinitionsSample();
        }
        /*
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Usage: AzureMonitorCSharpExamples <resourceId>");
            }

            var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            var secret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            if (new List<string> { tenantId, clientId, secret, subscriptionId }.Any(i => String.IsNullOrEmpty(i)))
            {
                Console.WriteLine("Please provide environment variables for AZURE_TENANT_ID, AZURE_CLIENT_ID, AZURE_CLIENT_SECRET and AZURE_SUBSCRIPTION_ID.");
            }
            else
            {
                readOnlyClient = AuthenticateWithReadOnlyClient(tenantId, clientId, secret, subscriptionId).Result;
                var resourceId = args[1];

                RunMetricDefinitionsSample(readOnlyClient, resourceId).Wait();
                RunMetricsSample(readOnlyClient, resourceId).Wait();
            }
        }

        #region Authentication
        private static async Task<MonitorClient> AuthenticateWithReadOnlyClient(string tenantId, string clientId, string secret, string subscriptionId)
        {
            // Build the service credentials and Monitor client
            var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, secret);
            var monitorClient = new MonitorClient(serviceCreds);
            monitorClient.SubscriptionId = subscriptionId;

            return monitorClient;
        }
        #endregion

        #region Examples
        private static async Task RunMetricDefinitionsSample(MonitorClient readOnlyClient, string resourceUri)
        {
            // Get metrics definitions
            IEnumerable<MetricDefinition> metricDefinitions = await readOnlyClient.MetricDefinitions.ListAsync(resourceUri: resourceUri, cancellationToken: new CancellationToken());
            EnumerateMetricDefinitions(metricDefinitions);

            // The $filter can contain a propositional logic expression, specifically, a disjunction of the format: name.value eq '<mericName>' or name.value eq '<metricName>' ...
            var odataFilterMetricDef = new ODataQuery<MetricDefinition>("name.value eq 'CpuPercentage'");
            metricDefinitions = await readOnlyClient.MetricDefinitions.ListAsync(resourceUri: resourceUri, odataQuery: odataFilterMetricDef, cancellationToken: new CancellationToken());

            EnumerateMetricDefinitions(metricDefinitions);
        }

        private static async Task RunMetricsSample(MonitorClient readOnlyClient, string resourceUri)
        {
            Write("Call without filter parameter (i.e. $filter = null)");
            IEnumerable<Metric> metrics = await readOnlyClient.Metrics.ListAsync(resourceUri: resourceUri, cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);

            // The $filter can contain a propositional logic expression, specifically, a disjunction of the format: [(]name.value eq '<mericName>' or name.value eq '<metricName>' ...[)]
            var metricNames = "name.value eq 'CpuPercentage'"; // could be concatenated with " or name.value eq '<another name>'" ... inside parentheses for more than one name.

            // The $filter can include time grain, which is optional when metricNames is present. The is forms a conjunction with the list of metric names described above.
            string timeGrain = " and timeGrain eq duration'PT5M'";

            // The $filter can also include a time range for the query; also a conjunction with the list of metrics and/or the time grain. Defaulting to 3 hours before the time of execution for these datetimes
            string startDate = string.Format(" and startTime eq {0}", DateTime.Now.AddHours(-3).ToString("o"));
            string endDate = string.Format(" and endTime eq {0}", DateTime.Now.ToString("o"));

            var odataFilterMetrics = new ODataQuery<Metric>(
                string.Format(
                    "{0}{1}{2}{3}",
                    metricNames,
                    timeGrain,
                    startDate,
                    endDate));

            Write("Call with filter parameter (i.e. $filter = {0})", odataFilterMetrics);
            metrics = await readOnlyClient.Metrics.ListAsync(resourceUri: resourceUri, odataQuery: odataFilterMetrics, cancellationToken: CancellationToken.None);
            EnumerateMetrics(metrics);
        }
        #endregion

        #region Helpers
        private static void Write(string format, params object[] items)
        {
            Console.WriteLine(string.Format(format, items));
        }

        private static void EnumerateMetricDefinitions(IEnumerable<MetricDefinition> metricDefinitions, int maxRecords = 5)
        {
            var numRecords = 0;
            foreach (var metricDefinition in metricDefinitions)
            {
                Write(
                    "Id: {0}\n Name: {1}, {2}\nResourceId: {3}\nUnit: {4}\nPrimary aggregation type: {5}\nList of metric availabilities: {6}",
                    metricDefinition.Id,
                    metricDefinition.Name.Value,
                    metricDefinition.Name.LocalizedValue,
                    metricDefinition.ResourceId,
                    metricDefinition.Unit,
                    metricDefinition.PrimaryAggregationType,
                    metricDefinition.MetricAvailabilities);

                // Display only maxRecords records at most
                numRecords++;
                if (numRecords >= maxRecords)
                {
                    break;
                }
            }
        }

        private static void EnumerateMetrics(IEnumerable<Metric> metrics, int maxRecords = 5)
        {
            var numRecords = 0;
            
            Write("Id\tName.Value\tName.Localized\tType\tUnit\tData");
            foreach (var metric in metrics)
            {
                Write(
                    "{0}\t{1}\t{2}\t{3}\t{4}\t{5}",
                    metric.Id,
                    metric.Name.Value,
                    metric.Name.LocalizedValue,
                    metric.Type,
                    metric.Unit,
                    metric.Data);

                // Display only 5 records at most
                numRecords++;
                if (numRecords >= maxRecords)
                {
                    break;
                }
            }
        }
        #endregion
        */
    }
}
