using System;
using System.Collections.Generic;
using System.Text;
using Client.WorkerNs;
using Microsoft.AspNetCore.Http.Connections;

namespace Client.ClientJobNs
{
    public class ClientJob
    {
        public int Id { get; set; }
        public ClientState State { get; set; }
        public Worker Client { get; set; } = Worker.SignalRCoreEcho;
        public int Connections { get; set; } = 256;
        public int Duration { get; set; } = 60;
        public int Interval { get; set; } = 1;
        public string Error { get; set; }
        public string ServerBenchmarkUri { get; set; }
        public HttpTransportType TransportType { get; set; } = HttpTransportType.WebSockets;
        public string HubProtocol { get; set; } = "json";
        public string Scenarios { get; set; }
        public string CallbackName { get; set; }

    }
}
