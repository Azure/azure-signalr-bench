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
    }
}
