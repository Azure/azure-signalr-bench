using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.Monitor.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;

namespace azuremonitor
{
    class AzureMonitor
    {
        private ArgsOption _argsOption;
        private IAzure _azure;

        public AzureMonitor(ArgsOption argsOption)
        {
            _argsOption = argsOption;
        }

        public void ListMetricDefinitionsSample()
        {
            foreach (var metricDefinition in _azure.MetricDefinitions.ListByResource(_argsOption.ResourceId))
            {
                Console.Write(
                    "\n\nId: {0}\n Name: {1}, {2}\nResourceId: {3}\nUnit: {4}\nPrimary aggregation type: {5}\nList of metric availabilities: {6}",
                    metricDefinition.Id,
                    metricDefinition.Name.Value,
                    metricDefinition.Name.LocalizedValue,
                    metricDefinition.ResourceId,
                    metricDefinition.Unit,
                    metricDefinition.PrimaryAggregationType,
                    metricDefinition.MetricAvailabilities);
            }
        }

        private void Dump(IMetricDefinition metricDefinition)
        {
            var metricCollection = metricDefinition.DefineQuery()
                                .StartingFrom(DateTime.Now.AddSeconds(-_argsOption.SecondsBeforeNow).ToUniversalTime())
                                .EndsBefore(DateTime.Now.ToUniversalTime())
                                .Execute();
            Console.WriteLine("Metrics for '" + _argsOption.ResourceId + "':");
            Console.WriteLine("Namespacse: " + metricCollection.Namespace);
            Console.WriteLine("Query time: " + metricCollection.Timespan);
            Console.WriteLine("Time Grain: " + metricCollection.Interval);
            Console.WriteLine("Cost: " + metricCollection.Cost);

            foreach (var metric in metricCollection.Metrics)
            {
                Console.WriteLine("\tMetric: " + metric.Name.LocalizedValue);
                Console.WriteLine("\tType: " + metric.Type);
                Console.WriteLine("\tUnit: " + metric.Unit);
                Console.WriteLine("\tTime Series: ");
                foreach (var timeElement in metric.Timeseries)
                {
                    Console.WriteLine("\t\tMetadata: ");
                    foreach (var metadata in timeElement.Metadatavalues)
                    {
                        Console.WriteLine("\t\t\t" + metadata.Name.LocalizedValue + ": " + metadata.Value);
                    }
                    Console.WriteLine("\t\tData: ");
                    foreach (var data in timeElement.Data)
                    {
                        Console.WriteLine("\t\t\t" + data.TimeStamp
                                + " : (Min) " + data.Minimum
                                + " : (Max) " + data.Maximum
                                + " : (Avg) " + data.Average
                                + " : (Total) " + data.Total
                                + " : (Count) " + data.Count);
                    }
                }
            }
        }

        public void QueryMetricDefinitionsSample()
        {
            var recordDateTime = DateTime.Now;
            foreach (var metricDefinition in _azure.MetricDefinitions.ListByResource(_argsOption.ResourceId))
            {
                if (string.Equals(metricDefinition.Name.Value, "CpuPercentage") ||
                    string.Equals(metricDefinition.Name.Value, "MemoryPercentage") ||
                    string.Equals(metricDefinition.Name.Value, "CpuTime"))
                {
                    Dump(metricDefinition);
                }
            }
        }

        public void Login()
        {
            var configLoader = new ConfigurationLoader();
            var sp = configLoader.Load<ServicePrincipal>(_argsOption.ServicePrincipal);
            var credentials = SdkContext.AzureCredentialsFactory
                    .FromServicePrincipal(sp.ClientId,
                    sp.ClientSecret,
                    sp.TenantId,
                    AzureEnvironment.AzureGlobalCloud);

            _azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(sp.Subscription);
        }
    }
}
