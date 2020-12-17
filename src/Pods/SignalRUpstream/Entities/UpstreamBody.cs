using System.Collections.Generic;

namespace SignalRUpstream.Entities
{
    public class UpstreamBody
    {
        public string Type { get; set; }
        public string Target { get; set; }
        public List<object> Arguments { get; set; }
    }
}