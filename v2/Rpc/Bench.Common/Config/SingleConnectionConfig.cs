using System.Collections.Generic;

namespace Bench.Common.Config
{
    public class SingleConnectionConfig
    {
        public double Duration { get; set; }
        public double Interval { get; set; }
        public string HubProptocol { get; set; }
        public string TransportType { get; set; }
        public string Stage { get; set; }
        public List<string> GroupNameList {get; set;}
        public bool Idle {get; set;}
        public string Method {get; set;}
    }
}