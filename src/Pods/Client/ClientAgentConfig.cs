using Azure.SignalRBench.Common;

namespace Azure.SignalRBench.Client
{
    public class ClientAgentConfig
    {
        public string Url { get; set; }
        public Protocol Protocol { get; set; }
        public string[] Groups { get; set; }
        public int GlobalIndex { get; set; }
        public bool ClientExpectServerAck { get; set; }
        public bool ServerExpectClientAck { get; set; }
    }
}