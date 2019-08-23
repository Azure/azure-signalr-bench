// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace MDMetricsClientSampleCode
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Cloud.Metrics.Client;
    using Microsoft.Cloud.Metrics.Client.Configuration;
    using Microsoft.Cloud.Metrics.Client.Metrics;
    using Microsoft.Cloud.Metrics.Client.Monitors;
    using Microsoft.Cloud.Metrics.Client.Query;
    using Microsoft.Cloud.Metrics.Client.MetricsExtension;
    using Microsoft.Online.Metrics.Serialization.Configuration;
    using Microsoft.Online.Metrics.Serialization.Monitor;
    using Newtonsoft.Json;

    /// <summary>
    /// The class for the sample code.
    /// </summary>
    internal class Program
    {
        private static void InteractiveRun()
        {
            // Suppress the informational logging from the MDM client API assembly.
            Microsoft.Cloud.Metrics.Client.Logging.Logger.SetMaxLogLevel(Microsoft.Cloud.Metrics.Client.Logging.LoggerLevel.Error);

            while (true)
            {
                Console.WriteLine(@"
                    Enter one of options:
                    0. Stop and exit.
                    1. Get a single time series.
                    2. Get a single time series with user authentication (AAD token).
                    3. Get multiple time series.
                    4. Get known time series definitions.
                    5. Get known time series definitions and then query.
                    6. Read the monitor information.
                    7. Read local raw metrics.
                    8. Read local aggregated metrics.
                    9. Get filtered dimension values.
                    10. Get filtered dimension values for multiple sampling types.
                    11. Get monitoring account and metric configurations.
                    12. Read MetricsExtension diagnostic events.
                    13. Create Monitoring Account.
                    14. All scenarios but reading local raw metrics.
                    15. Test Dogfood PodMemory.
                    16. Test Dogfood PodCpuUsage.
                    ");

                uint option;
                while (!uint.TryParse(Console.ReadLine(), out option))
                {
                    Console.Write("The value must be of unsigned integer type, try again: ");
                }

                switch (option)
                {
                    case 0:
                        return;

                    case 1:
                        GetSingleTimeSeries(useUserAuth: false);
                        break;

                    case 2:
                        GetSingleTimeSeries(useUserAuth: true);
                        break;

                    case 3:
                        GetMultipleTimeSeriesWithResolutionReduction();
                        break;

                    case 4:
                        GetKnownTimeSeriesDefinitions();
                        break;

                    case 5:
                        GetKnownTimeSeriesDefinitionsAndQuery();
                        break;

                    case 6:
                        ReadMonitorInfo();
                        break;

                    case 7:
                        ReadLocalRawMetrics();
                        break;

                    case 8:
                        ReadLocalAggregatedMetrics();
                        break;

                    case 9:
                        GetFilteredDimensionValues();
                        break;

                    case 10:
                        GetFilteredDimensionValuesV3();
                        break;

                    case 11:
                        GetConfigurations();
                        break;

                    case 12:
                        ReadMetricExtensionDiagnosticEvents();
                        break;

                    case 13:
                        CreateMonitoringAccount();
                        break;

                    case 14:
                        GetSingleTimeSeries(useUserAuth: false);
                        GetSingleTimeSeries(useUserAuth: true);
                        GetMultipleTimeSeriesWithResolutionReduction();
                        GetKnownTimeSeriesDefinitions();
                        GetKnownTimeSeriesDefinitionsAndQuery();
                        GetFilteredDimensionValues();
                        GetFilteredDimensionValuesV3();
                        ReadMonitorInfo();
                        ReadLocalAggregatedMetrics();
                        ReadMetricExtensionDiagnosticEvents();
                        GetConfigurations();
                        CreateMonitoringAccount();
                        break;

                    case 15:
                        GetDogfoodMemorySeries();
                        break;

                    case 16:
                        GetDogfoodCPUSeries();
                        break;

                    default:
                        Console.WriteLine("Invalid option: {0}! ", option);
                        break;
                }
            }
        }
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                InteractiveRun();
            }
            else if (args.Length == 6)
            {
                if (!Enum.TryParse<PlatformType>(args[1], out var platformType))
                {
                    Console.WriteLine($"Invalid platform type: {args[1]}");
                }
                if (!Enum.TryParse<SystemLoadType>(args[2], out var systemLoadType))
                {
                    Console.WriteLine($"Invalid systemload type: {args[2]}");
                }
                var dateStart = args[3];
                var dateEnd = args[4];
                if (DateTime.TryParse(dateStart, out DateTime normalDateStart))
                {
                    Console.WriteLine($"Successfully parse the start date: {normalDateStart}");
                }
                else
                {
                    Console.WriteLine($"Fail to parse the start date: {dateStart}");
                }
                if (DateTime.TryParse(dateEnd, out DateTime normalDateEnd))
                {
                    Console.WriteLine($"Successfully parse the end date: {normalDateEnd}");
                }
                else
                {
                    Console.WriteLine($"Fail to parse the end date: {dateEnd}");
                }
                Console.WriteLine(MDMQuery.QueryMetrics(args[0], platformType, systemLoadType, normalDateStart, normalDateEnd, args[5]));
            }
        }

        public static void GetDogfoodMemorySeries()
        {
            var testCertificateThumbprint = "C35CBFF9FA6C51E51E1DE97B6D1E246F27661301";
            var httpsUrl = "https://shoebox2.metrics.nsatc.net/public/monitoringAccount/SignalRShoeboxTest/homeStamp";
            var connectionInfo = new ConnectionInfo(new Uri(httpsUrl), testCertificateThumbprint, StoreLocation.LocalMachine);
            Console.WriteLine(connectionInfo.Timeout);
            //Console.WriteLine(connectionInfo.MdmEnvironment);
            var reader = new MetricReader(connectionInfo);

            // Single metric 
            var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", "PodMemory");
            //var id = new MetricIdentifier("SignalRShoeboxTest", "ShoeboxInternal", "MessageCountRaw");
            //var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", "PodMemory");

            // The short link for this series is http://jarvis-int.dc.ad.msft.net/D10A9E2E.
            var definition = new TimeSeriesDefinition<MetricIdentifier>(
                id,
                new Dictionary<string, string> { //{ "ResourceId", resourceId}
                    { "resourceKubeId", "62a558c2-2895-423d-a7b0-05b03a15b65a"}
                });

            TimeSeries<MetricIdentifier, double?> result =
                reader.GetTimeSeriesAsync(DateTime.UtcNow.AddHours(-10), DateTime.UtcNow, SamplingType.Max, definition).Result;

            foreach (var dataPoint in result.Datapoints)
            {
                Console.WriteLine("Time: {0}, Value: {1}", dataPoint.TimestampUtc, dataPoint.Value);
            }

            Console.WriteLine("############################ END OF GetDogfoodSeries ##############################");
        }

        public static void GetDogfoodCPUSeries()
        {
            var testCertificateThumbprint = "C35CBFF9FA6C51E51E1DE97B6D1E246F27661301";
            var httpsUrl = "https://shoebox2.metrics.nsatc.net/public/monitoringAccount/SignalRShoeboxTest/homeStamp";
            var connectionInfo = new ConnectionInfo(new Uri(httpsUrl), testCertificateThumbprint, StoreLocation.LocalMachine);
            var reader = new MetricReader(connectionInfo);

            // Single metric 
            var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", "PodCpuUsage");
            //var id = new MetricIdentifier("SignalRShoeboxTest", "ShoeboxInternal", "MessageCountRaw");
            //var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", "PodMemory");

            // The short link for this series is http://jarvis-int.dc.ad.msft.net/D10A9E2E.
            var definition = new TimeSeriesDefinition<MetricIdentifier>(
                id,
                new Dictionary<string, string> { //{ "ResourceId", resourceId}
                    { "resourceKubeId", "62a558c2-2895-423d-a7b0-05b03a15b65a"}
                });

            TimeSeries<MetricIdentifier, double?> result =
                reader.GetTimeSeriesAsync(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, SamplingType.Max, definition).Result;

            foreach (var dataPoint in result.Datapoints)
            {
                Console.WriteLine("Time: {0}, Value: {1}", dataPoint.TimestampUtc, dataPoint.Value);
            }
            Console.WriteLine(JsonConvert.SerializeObject(result));
            Console.WriteLine("############################ END OF GetDogfoodSeries ##############################");
        }
        /// <summary>
        /// Gets the single time series.
        /// </summary>
        /// <param name="useUserAuth">if set to <c>true</c>, use user authentication; otherwise use certificate authentication.</param>
        public static void GetSingleTimeSeries(bool useUserAuth)
        {
            ConnectionInfo connectionInfo;
            if (useUserAuth)
            {
                connectionInfo = new ConnectionInfo(MdmEnvironment.Int);
            }
            else
            {
                // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
                // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
                // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
                string testCertificateThumbprint = "C35CBFF9FA6C51E51E1DE97B6D1E246F27661301";
                connectionInfo = new ConnectionInfo(new Uri("https://shoebox2.metrics.nsatc.net/public/monitoringAccount/SignalRShoeboxTest/homeStamp"), testCertificateThumbprint, StoreLocation.LocalMachine);
            }

            var reader = new MetricReader(connectionInfo);

            // Single metric 
            var id = new MetricIdentifier("SignalRShoeboxTest", "ShoeboxInternal", "ConnectionCountDelta");
            //var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", "PodMemory");

            // The short link for this series is http://jarvis-int.dc.ad.msft.net/D10A9E2E.
            var definition = new TimeSeriesDefinition<MetricIdentifier>(
                id,
                new Dictionary<string, string> { { "ResourceId", "/subscriptions/5ea15035-434e-46ba-97cd-ea0927a47104/resourceGroups/testsoutheastasia/providers/Microsoft.SignalRService/SignalR/honzhangcenable3" } });

            TimeSeries<MetricIdentifier, double?> result =
                reader.GetTimeSeriesAsync(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, SamplingType.Average, definition).Result;

            foreach (var dataPoint in result.Datapoints)
            {
                Console.WriteLine("Time: {0}, Value: {1}", dataPoint.TimestampUtc, dataPoint.Value);
            }

            Console.WriteLine("############################ END OF GetSingleTimeSeries ##############################");
        }

        /// <summary>
        /// Gets the multiple time series with resolution reduction.
        /// </summary>
        public static void GetMultipleTimeSeriesWithResolutionReduction()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, MdmEnvironment.Int);
            var reader = new MetricReader(connectionInfo);

            var id1 = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Memory\\Available MBytes");
            var id2 = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Processor(_Total)\\% Processor Time");

            var dimensionCombination = new Dictionary<string, string> { { "Datacenter", "westus" }, { "__Role", "metrics.server" } };
            var definitions = new List<TimeSeriesDefinition<MetricIdentifier>>();
            definitions.Add(new TimeSeriesDefinition<MetricIdentifier>(id1, dimensionCombination));
            definitions.Add(new TimeSeriesDefinition<MetricIdentifier>(id2, dimensionCombination));

            const int seriesResolutionInMinutes = 5;
            var endTimeUtc = DateTime.UtcNow;
            var result = reader.GetMultipleTimeSeriesAsync(endTimeUtc.AddMinutes(-9), endTimeUtc, SamplingType.Average, seriesResolutionInMinutes, definitions).Result;

            foreach (var series in result)
            {
                foreach (var dataPoint in series.Datapoints)
                {
                    Console.WriteLine(
                        "Metric: {0}, Time: {1}, Value: {2}",
                        series.Definition.Id.MetricName,
                        dataPoint.TimestampUtc,
                        dataPoint.Value);
                }
            }

            result =
                reader.GetMultipleTimeSeriesAsync(
                    endTimeUtc.AddMinutes(-9),
                    endTimeUtc,
                    new[] { SamplingType.Average },
                    definitions,
                    seriesResolutionInMinutes,
                    AggregationType.None).Result;

            foreach (var series in result)
            {
                foreach (var dataPoint in series.Datapoints)
                {
                    Console.WriteLine(
                        "Metric: {0}, Time: {1}, Value: {2}",
                        series.Definition.Id.MetricName,
                        dataPoint.TimestampUtc,
                        dataPoint.Value);
                }
            }

            Console.WriteLine("############################ END OF GetMultipleTimeSeriesWithResolutionReduction ##############################");
        }

        /// <summary>
        /// Gets the known time series definitions.
        /// </summary>
        public static void GetKnownTimeSeriesDefinitions()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, TimeSpan.FromSeconds(300), MdmEnvironment.Int);

            var reader = new MetricReader(connectionInfo);

            string monitoringAccount = "MetricTeamInternalMetrics";
            IReadOnlyList<string> namespaces = reader.GetNamespacesAsync(monitoringAccount).Result;

            string metricNamespace = "PlatformMetrics";
            IReadOnlyList<string> metricNames = reader.GetMetricNamesAsync(monitoringAccount, metricNamespace).Result;

            string metric = "\\Memory\\Available MBytes";
            var id = new MetricIdentifier(monitoringAccount, metricNamespace, metric);

            var dimensions = reader.GetDimensionNamesAsync(id).Result;
            Console.WriteLine("Dimensions are: {0}", string.Join(", ", dimensions));

            var preaggregates = reader.GetPreAggregateConfigurationsAsync(id).Result;
            foreach (var preAggregateConfiguration in preaggregates)
            {
                Console.WriteLine("Pre-aggregate: {0}", JsonConvert.SerializeObject(preAggregateConfiguration));
            }

            var knownTimeSeriesDefinitions =
               reader.GetKnownTimeSeriesDefinitionsAsync(id, "__Role", DimensionFilter.CreateExcludeFilter("Datacenter", "eastus2")).Result;

            var roleIndex = knownTimeSeriesDefinitions.GetIndexInDimensionCombination("__Role");
            Console.WriteLine("The index of the '__Role' dimension is {0}.", roleIndex);

            foreach (var value in knownTimeSeriesDefinitions)
            {
                Console.WriteLine("Known time series definition: {0}", JsonConvert.SerializeObject(value.DimensionCombination));
            }

            Console.WriteLine("############################ END OF GetKnownTimeSeriesDefinitions ##############################");
        }

        /// <summary>
        /// Gets the known time series definitions and query.
        /// </summary>
        public static void GetKnownTimeSeriesDefinitionsAndQuery()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, MdmEnvironment.Int);

            var reader = new MetricReader(connectionInfo);

            var id = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Memory\\Available MBytes");

            // Use implicit conversion from string for simple case.
            IReadOnlyList<TimeSeriesDefinition<MetricIdentifier>> knownDimensions =
                reader.GetKnownTimeSeriesDefinitionsAsync(id, "__Role", "Datacenter").Result;

            TimeSeries<MetricIdentifier, double?> result =
                reader.GetTimeSeriesAsync(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, SamplingType.Sum, knownDimensions.First()).Result;

            foreach (var dataPoint in result.Datapoints)
            {
                Console.WriteLine("Time: {0}, Value: {1}", dataPoint.TimestampUtc, dataPoint.Value);
            }

            Console.WriteLine("############################ END OF GetKnownTimeSeriesDefinitionsAndQuery ##############################");
        }

        /// <summary>
        /// Gets the known dimension combinations that match the query criteria.  In this example, we are checking for available memory
        /// on all our roles and datacenters.
        /// </summary>
        public static void GetFilteredDimensionValues()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, MdmEnvironment.Int);

            var reader = new MetricReader(connectionInfo);

            var id = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Memory\\Available MBytes");

            var dimensionFilters = new DimensionFilter[] { "__Role", "Datacenter" };

            IEnumerable<IQueryResult> results = reader.GetFilteredDimensionValuesAsync(
                id,
                dimensionFilters,
                DateTime.UtcNow.AddMinutes(-60),
                DateTime.UtcNow,
                SamplingType.Sum,
                Reducer.Average,
                new QueryFilter(Operator.GreaterThan, 5000),
                false,
                new SelectionClause(SelectionType.TopValues, 10, OrderBy.Descending)
                ).Result;

            foreach (var series in results)
            {
                IEnumerable<string> dimensions = series.DimensionList.Select(x => string.Format("[{0}, {1}]", x.Key, x.Value));
                Console.WriteLine("Dimensions: {0}, Evaluated Result: {1:N0}", string.Join(", ", dimensions), series.EvaluatedResult);
            }

            Console.WriteLine("############################ END OF GetFilteredDimensionValues ##############################");
        }

        /// <summary>
        /// Gets the known dimension combinations that match the query criteria and the associated time series.  
        /// In this example, we are checking the sum and count for available memory on all our roles and datacenters.
        /// </summary>
        public static void GetFilteredDimensionValuesV3()
        {
            var connectionInfo = new ConnectionInfo(MdmEnvironment.Int);

            var reader = new MetricReader(connectionInfo);

            var id = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Memory\\Available MBytes");

            var dimensionFilters = new DimensionFilter[] { "__Role", "Datacenter" };

            IQueryResultListV3 results = reader.GetFilteredDimensionValuesAsyncV3(
                id,
                dimensionFilters,
                DateTime.UtcNow.AddMinutes(-10),
                DateTime.UtcNow,
                new[] { SamplingType.Sum, SamplingType.Count },
                new SelectionClauseV3(new PropertyDefinition(PropertyAggregationType.Average, SamplingType.Sum), 10, OrderBy.Descending)).Result;

            foreach (var series in results.Results)
            {
                IEnumerable<string> dimensions = series.DimensionList.Select(x => string.Format("[{0}, {1}]", x.Key, x.Value));
                var sumSeries = $"[{string.Join(",", series.GetTimeSeriesValues(SamplingType.Sum))}]";
                var countSeries = $"[{string.Join(",", series.GetTimeSeriesValues(SamplingType.Count))}]";
                Console.WriteLine("Dimensions: {0}, \n\tSum: {1}, \n\tCount: {2}.", string.Join(", ", dimensions), sumSeries, countSeries);
            }

            Console.WriteLine("############################ END OF GetFilteredDimensionValues ##############################");
        }

        /// <summary>
        /// Reads the monitor information.
        /// </summary>
        public static void ReadMonitorInfo()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, MdmEnvironment.Int);

            var reader = new MonitorReader(connectionInfo);

            var id = new MetricIdentifier("MetricTeamInternalMetrics", "PlatformMetrics", "\\Memory\\Available MBytes");
            IReadOnlyList<MonitorIdentifier> result = reader.GetMonitorsAsync(id).Result;
            Console.WriteLine("There are {0} monitors under {1} - {2}.\n", result.Count, id.MetricName, JsonConvert.SerializeObject(result));

            var allMonitors = reader.GetMonitorsAsync(id.MonitoringAccount).Result;
            Console.WriteLine("There are {0} monitors under {1} - {2}.\n", allMonitors.Count, id.MonitoringAccount, JsonConvert.SerializeObject(allMonitors));

            var definition = new TimeSeriesDefinition<MonitorIdentifier>(
                result[0],
                new Dictionary<string, string> { { "Datacenter", "westus" }, { "__Role", "metrics.server" } });

            IMonitorHealthStatus result2 = reader.GetCurrentHeathStatusAsync(definition).Result;
            Console.WriteLine("The current health status is {0}.\n", JsonConvert.SerializeObject(result2));

            var definition2 = new TimeSeriesDefinition<MonitorIdentifier>(
                result[0],
                new Dictionary<string, string> { { "Datacenter", "eastus2" }, { "__Role", "metrics.server" } });

            var statuses = reader.GetMultipleCurrentHeathStatusesAsync(definition, definition2).Result;
            Console.WriteLine("The current health statuses are: \n{0}.\n{1}.\n ", JsonConvert.SerializeObject(statuses[0]), JsonConvert.SerializeObject(statuses[1]));

            // The short link for the history is http://jarvis-int.dc.ad.msft.net/44EA28BD
            TimeSeries<MonitorIdentifier, bool?> result3 =
                reader.GetMonitorHistoryAsync(DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow, definition).Result;

            Console.WriteLine("The monitor health history is:");
            foreach (var datapoint in result3.Datapoints)
            {
                Console.WriteLine("Time: {0}, Result: {1}", datapoint.TimestampUtc, datapoint.Value);
            }

            Console.WriteLine("############################ END OF ReadMonitorInfo ##############################");
        }

        /// <summary>
        /// Reads the local raw metrics.
        /// </summary>
        public static void ReadLocalRawMetrics()
        {
            var reader = new LocalMetricReader();

            var source = new CancellationTokenSource();
            Task rawMetrics = reader.ReadLocalRawMetricsAsync(m => Console.WriteLine("ReceivedRaw: " + JsonConvert.SerializeObject(m)), source.Token);

            TimeSpan waitTime = TimeSpan.FromSeconds(60);
            Console.WriteLine("Control returned. Wait for {0}...", waitTime);

            // Let it run for a while
            Thread.Sleep(waitTime);

            Console.WriteLine("Cancel");
            // Cancel/wait
            source.Cancel();

            Console.WriteLine("Wait for completion");
            rawMetrics.Wait();

            Console.WriteLine("Done");

            Console.WriteLine("############################ END OF ReadLocalRawMetrics ##############################");
        }

        /// <summary>
        /// Reads the local aggregated metrics.
        /// </summary>
        public static void ReadLocalAggregatedMetrics()
        {
            var reader = new LocalMetricReader();

            var source = new CancellationTokenSource();

            Task aggregatedMetrics = reader.ReadLocalAggregatedMetricsAsync(m => Console.WriteLine("ReceivedAgg: " + JsonConvert.SerializeObject(m)), source.Token);

            TimeSpan waitTime = TimeSpan.FromSeconds(120);
            Console.WriteLine("Control returned. Wait for {0}", waitTime);

            // Let it run for a while
            Thread.Sleep(waitTime);

            Console.WriteLine("Cancel");
            // Cancel/wait
            source.Cancel();

            Console.WriteLine("Wait for completion");
            aggregatedMetrics.Wait();

            Console.WriteLine("Done");

            Console.WriteLine("############################ END OF ReadLocalMetrics ##############################");
        }

        /// <summary>
        /// Reads the metric extension diagnostic events.
        /// </summary>
        public static void ReadMetricExtensionDiagnosticEvents()
        {
            var reader = new DiagnosticHeartbeatReader();

            var source = new CancellationTokenSource();

            Task aggregatedMetrics = reader.ReadDiagnosticHeartbeatsAsync(m => Console.WriteLine("ReceivedHeartbeat: " + JsonConvert.SerializeObject(m)), source.Token);

            TimeSpan waitTime = TimeSpan.FromSeconds(120);
            Console.WriteLine("Control returned. Wait for {0}", waitTime);

            // Let it run for a while
            Thread.Sleep(waitTime);

            Console.WriteLine("Cancel");
            // Cancel/wait
            source.Cancel();

            Console.WriteLine("Wait for completion");
            aggregatedMetrics.Wait();

            Console.WriteLine("Done");

            Console.WriteLine("############################ END OF ReadMetricExtensionDiagnosticEvents ##############################");
        }

        /// <summary>
        /// This method demonstrates how this api can be used to get monitoring account and metric configuration information.
        /// These objects can be modified and saved as well, which is not demonstrated here due to permissions.
        /// </summary>
        public static void GetConfigurations()
        {
            // Replace 31280E2F2D2220808315C212DF8062A295B28325 with your cert thumbprint,
            // install it to the "Personal\Certificates" folder in the "Local Computer" certificate store,
            // and grant the permission of reading the private key to the service/application using the MDM consumption APIs.
            string testCertificateThumbprint = "31280E2F2D2220808315C212DF8062A295B28325";

            var connectionInfo = new ConnectionInfo(testCertificateThumbprint, StoreLocation.LocalMachine, MdmEnvironment.Int);

            var manager = new MonitoringAccountConfigurationManager(connectionInfo);

            var account = manager.GetAsync("MetricTeamInternalMetrics").Result;

            Console.WriteLine("------- ACCOUNT INFORMATION -------");
            Console.WriteLine("Users: {0}", string.Join(", ", account.Permissions.Where(x => x is UserPermission).Select(x => x.Identity)));
            Console.WriteLine("SecurityGroups: {0}", string.Join(", ", account.Permissions.Where(x => x is SecurityGroup).Select(x => x.Identity)));
            Console.WriteLine("Certificates: {0}", string.Join(", ", account.Permissions.Where(x => x is Certificate).Select(x => x.Identity)));

            var metricConfigurationManager = new MetricConfigurationManager(connectionInfo);

            var metric = metricConfigurationManager.GetAsync(account, "metrics.server", "clientPostCount").Result;
            var rawMetric = (RawMetricConfiguration)metric;

            Console.WriteLine("\n------- RAW METRIC INFORMATION -------");
            Console.WriteLine("Dimensions: {0}", string.Join(",", rawMetric.Dimensions));
            Console.WriteLine("Preaggregates: {0}", string.Join(",", rawMetric.Preaggregations.Select(x => x.Name)));

            metric = metricConfigurationManager.GetAsync(account, "metrics.server", "AccountThrottlingWatchdog").Result;
            var compositeMetric = (CompositeMetricConfiguration)metric;

            Console.WriteLine("\n------- COMPOSITE METRIC INFORMATION -------");

            Console.WriteLine("CompositeExpressions: {0}", string.Join(",", compositeMetric.CompositeExpressions.Select(x => x.Expression)));
            Console.WriteLine(
                "FirstMetricSource: {0} - {1} - {2}",
                compositeMetric.MetricSources.First().MonitoringAccount,
                compositeMetric.MetricSources.First().MetricNamespace,
                compositeMetric.MetricSources.First().Metric);

            var metricsServerMetrics = metricConfigurationManager.GetMultipleAsync(account, "Metrics.Server").Result;

            Console.WriteLine("\n------- MULTIPLE METRIC INFORMATION -------");
            Console.WriteLine("Name of all metrics under Metrics.Server namespace: {0}", string.Join("\n", metricsServerMetrics.Select(x => x.Name)));

            Console.WriteLine("\n############################ END OF GetConfigurations ##############################");
        }

        /// <summary>
        /// This method demonstrates how to create monitoring account for those with permissions.
        /// </summary>
        public static void CreateMonitoringAccount()
        {
            var connectionInfo = new ConnectionInfo(MdmEnvironment.Int);

            var manager = new MonitoringAccountConfigurationManager(connectionInfo);

            var newAccountToCreate = "NewAccount";
            Console.WriteLine($"Create a new monitoring account '{newAccountToCreate}' from scratch...");

            var permissions = new IPermissionV2[] { new UserPermissionV2("my-alias", RoleConfiguration.Administrator) };
            var monitoringAccount = new MonitoringAccount(newAccountToCreate, "test account creation", permissions);
            try
            {
                manager.CreateAsync(monitoringAccount, "int2.metrics.nsatc.net").Wait();
                Console.WriteLine($"{newAccountToCreate} is successfully created.");
            }
            catch (AggregateException e)
            {
                var inner = e.InnerException as ConfigurationValidationException;
                if (inner?.Message?.IndexOf($"{newAccountToCreate} is already in use", StringComparison.Ordinal) >= 0)
                {
                    Console.WriteLine($"{newAccountToCreate} is already in use.");
                }
                else
                {
                    throw;
                }
            }

            newAccountToCreate = "NewAccount2";
            var monitoringAccountToCopyFrom = "NewAccount";
            Console.WriteLine($"Create a new monitoring account named '{newAccountToCreate}' by copying the common settings from '{monitoringAccountToCopyFrom}'...");

            try
            {
                manager.CreateAsync(newAccountToCreate, monitoringAccountToCopyFrom, "int2.metrics.nsatc.net").Wait();
                Console.WriteLine($"{newAccountToCreate} is successfully created.");
            }
            catch (AggregateException e)
            {
                var inner = e.InnerException as ConfigurationValidationException;
                if (inner?.Message?.IndexOf($"{newAccountToCreate} is already in use", StringComparison.Ordinal) >= 0)
                {
                    Console.WriteLine($"{newAccountToCreate} is already in use.");
                }
                else
                {
                    throw;
                }
            }
        }
    }
}