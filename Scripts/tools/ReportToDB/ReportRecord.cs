namespace ReportToDB
{
    public class ReportRecord
    {
        public string Timestamp { get; set; }
        public string Scenario { get; set; }
        public int Connections { get; set; }
        public int Sends { get; set; }
        public long SendTPuts { get; set; }
        public long RecvTPuts { get; set; }
        public string Reference { get; set; }

        public bool HasConnectionStat { get; set; }
        public int DroppedConnections { get; set; }
        public int ReconnCost99Percent { get; set; }
        public int LifeSpan99Percent { get; set; }
        public int Offline99Percent { get; set; }

        public string Others { get; set; }
    }
}
