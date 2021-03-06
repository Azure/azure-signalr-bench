﻿using Common;
using Newtonsoft.Json.Linq;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class CollectStatistics : CollectStatisticBase, IMasterMethod
    {
        public Task Do(IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters, IList<IRpcClient> clients)
        {
            Log.Information($"Start to collect statistics...");
            CollectStatistics(stepParameters, pluginParameters, clients, LatencyEventerCallback);
            return Task.CompletedTask;
        }
    }
}
