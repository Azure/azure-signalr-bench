namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class BenchMessage
    {
        public string MessageBlob { get; set; }
        public long Timestamp { get; set; }
        public string Target { get; set; }

        private bool _isInGroup;

        public bool GetIsInGroup() => _isInGroup;

        public void SetIsInGroup(bool value) => _isInGroup = value;

        public BenchMessage Clone() => (BenchMessage)MemberwiseClone();
    }
}
