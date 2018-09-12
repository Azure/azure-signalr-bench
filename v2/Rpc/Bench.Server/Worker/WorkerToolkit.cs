using System;
using System.Collections.Generic;
using System.Net.Http;
using Bench.Common;
using Bench.Common.Config;
using Bench.RpcSlave.Worker.Counters;
using Bench.RpcSlave.Worker.Savers;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bench.RpcSlave.Worker
{
    public class WorkerToolkit
    {
        public JobConfig JobConfig { get; set; }
        public List<HubConnection> Connections { get; set; }
        public List<IDisposable> ConnectionCallbacks { get; set; } = new List<IDisposable>();
        public Stat.Types.State State { get; set; } = Stat.Types.State.WorkerUnexist;
        public Counter Counters { get; set; } = new Counter(new LocalFileSaver());
        public Common.BenchmarkCellConfig BenchmarkCellConfig { get; set; }
        public int ServerCount { get; set; }

        public ConnectionConfigList ConnectionConfigList { get; set; }
        public Range ConnectionRange { get; set; }

        public Dictionary<string, bool> Init { get; set; } = new Dictionary<string, bool>();

        public List<string> ConnectionIds { get; set; } = new List<string>();

        // serverless mode needs connection string
        public string ConnectionString { get; set; }
        // Many HttpClient are used to post REST message to service.
        // Why do we use many HttpClient instead of only one instance?
        //   To solve unbalance issue on service side.
        //   Otherwise, single httpclient sends quite a lot message to single
        // service instance pod will overload that pod.
        // Tips: Every httpclient should set CookieContainer, then the HttpClients
        // request will be dispatched to every service pod in balanced way.
        public List<HttpClient> HttpClients { get; set; }
    }
}