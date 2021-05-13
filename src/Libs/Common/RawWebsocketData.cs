namespace Azure.SignalRBench.Common
{
    public class RawWebsocketData
    {
        public string Type { get; set; }= "";

        public string Target { get; set; } = "";
        public string Payload { get; set; } = "";
        public long Ticks { get; set; }
        
    }
}