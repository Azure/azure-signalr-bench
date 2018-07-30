using Microsoft.Cloud.Metrics.Client;
using Microsoft.Cloud.Metrics.Client.Metrics;
using Microsoft.Online.Metrics.Serialization.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MDMetricsClientSampleCode
{
    public class MDMQuery
    {
        public static string QueryMetrics(string podName, PlatformType platformType, SystemLoadType systemLoadType, DateTime startTime, DateTime endTime, string filePath)
        {
            if (platformType == PlatformType.Dogfood)
            {
                var testCertificateThumbprint = "C35CBFF9FA6C51E51E1DE97B6D1E246F27661301";
                var httpsUrl = "https://shoebox2.metrics.nsatc.net/public/monitoringAccount/SignalRShoeboxTest/homeStamp";
                var connectionInfo = new ConnectionInfo(new Uri(httpsUrl), testCertificateThumbprint, StoreLocation.LocalMachine);
                var id = new MetricIdentifier("SignalRShoeboxTest", "systemLoad", systemLoadType == SystemLoadType.CPU ? "PodCpuUsage" : "PodMemory");
                var reader = new MetricReader(connectionInfo);

                // The short link for this series is http://jarvis-int.dc.ad.msft.net/D10A9E2E.
                var definition = new TimeSeriesDefinition<MetricIdentifier>(
                    id,
                    new Dictionary<string, string> {
                    { "podName", podName}
                    });

                TimeSeries<MetricIdentifier, double?> result =
                    reader.GetTimeSeriesAsync(startTime, endTime, SamplingType.Max, definition).Result;
                var strOutput = JsonConvert.SerializeObject(result);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath, false))
                {
                    file.Write(strOutput);
                }
                return strOutput;
            }
            return null;
        }
    }
}
