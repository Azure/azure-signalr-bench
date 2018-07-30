using Client.ClientJobNs;
using Client.StartTimeOffsetGenerator;
using Client.Statistics;
using Client.Statistics.Savers;
using Client.UtilNs;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Client.Tools
{
    public class BaseTool: ITool
    {
        public List<HubConnection> Connections;
        public ClientJob Job;
        public long StartTimestamp;
        public List<int> SentMassage;
        public string CallbackName;

        public BaseTool()
        {
        }
    }
}
