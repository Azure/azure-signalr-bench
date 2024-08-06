using Prometheus;

namespace Azure.SignalRBench.Client
{
    public class ClientMetrics
    {
        private static string[] _labelNames = new []{ "testName", "index" };
        private static Gauge? _connectedClientCount =Metrics
            .CreateGauge("client_connected", "Number of connected clients.", new GaugeConfiguration
            {
                SuppressInitialValue = true,
                LabelNames = _labelNames
            });
        private static Counter? _clientReconnectCount =Metrics.CreateCounter("client_reconnect", "Number of reconnects of clients.", new CounterConfiguration
        {
            SuppressInitialValue = true,
            LabelNames = _labelNames
        });
        private static Histogram? _clientLatency =Metrics.CreateHistogram("client_latency", "Latency of clients.", new HistogramConfiguration
        {
            SuppressInitialValue = true,
            // use default buckets
            LabelNames = _labelNames
        });

        private static string? _testName;
        private static string? _index;
        private static string? _podName;
        
        public static void Init(string testName, string index)
        {
            _testName = testName;
            _index = index;
        }
        
        public static void SetConnectedClientCount(double value)
        {
            _connectedClientCount.WithLabels(_testName, _index, _podName).Set(value);
        }
        
        public static void IncClientReconnectCount()
        {
            _clientReconnectCount.WithLabels(_testName, _index, _podName).Inc();
        }
        
        public static void ObserveClientLatency(double value)
        {
            _clientLatency.WithLabels(_testName, _index, _podName).Observe(value);
        }
    }
}