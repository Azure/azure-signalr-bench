using System;
using System.Collections.Generic;
using System.Text;
using JenkinsScript.Config.FinerConfigs;
using JenkinsScript.FinerConfigs;

namespace JenkinsScript
{
    public class JobConfigV2
    {
        // common config

        public string ServiceType { get; set; }
        public string TransportType { get; set; }
        public string HubProtocol { get; set; }
        public string Scenario { get; set; }
        public int Connection { get; set; }
        public int ConcurrentConnection {get; set;}
        public int Duration { get; set; }
        public int Interval { get; set; }
        public int GroupNum { get; set; }
        public int Overlap { get; set; }
        public bool IsGroupJoinLeave { get; set; }
        public List<string> Pipeline { get; set; }
        public string ServerUrl { get; set; }

    }
}