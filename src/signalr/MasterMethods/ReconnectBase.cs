﻿using Common;
using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.MasterMethods
{
    public class ReconnectBase
    {
        protected Task Reconnect(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters,
            IList<IRpcClient> clients)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.ConcurrentConnection, out int concurrentConnection, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
            // Prepare configuration for each clients
            var packages = clients.Select((client, i) =>
            {
                int currentConcurrentConnection = Util.SplitNumber(concurrentConnection, i, clients.Count);
                (int beg, int end) = Util.GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object>(stepParameters);
                data[SignalRConstants.ConcurrentConnection] = currentConcurrentConnection > 0 ? currentConcurrentConnection : 1;
                return new { Client = client, Data = data };
            });

            // Process on clients
            var results = from package in packages select package.Client.QueryAsync(package.Data);
            var task = Task.WhenAll(results);
            long expectedMilliseconds = SignalRConstants.MillisecondsToWait;
            if (SignalRUtils.FetchTotalConnectionFromContext(pluginParameters, type, out int totalConnections))
            {
                expectedMilliseconds = SignalRUtils.GetTimeoutPerConcurrentSpeed(totalConnections, concurrentConnection);
            }

            return Util.TimeoutCheckedTask(task, expectedMilliseconds, nameof(Reconnect));
        }
    }
}
