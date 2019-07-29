namespace JenkinsScript
{
    public class PrivateIpConfig
    {
        public string ServicePrivateIp { get; set; }
        public string AppServerPrivateIp { get; set; }
        public string MasterPrivateIp { get; set; }
        public string AgentPrivateIp { get; set; }
        public string BenchPrivateIp { get; set; }
    }
}