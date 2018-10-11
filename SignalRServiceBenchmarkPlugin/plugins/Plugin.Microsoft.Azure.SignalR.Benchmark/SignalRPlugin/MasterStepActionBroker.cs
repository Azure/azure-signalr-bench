using Common;
using Newtonsoft.Json;
using Plugin.Base;
using Rpc.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    public class MasterStepActionBroker
    {
        public Task CreateConnection(IDictionary<string, object> parameters, IList<IRpcClient> clients)
        {
            parameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int connectionTotal, Convert.ToInt32);

            var datas = clients.Select((client, i) => {
                (int beg, int end) = GetConnectionRange(connectionTotal, i, clients.Count);
                var data = new Dictionary<string, object> { { SignalRConstants.ConnectionBegin, beg }, { SignalRConstants.ConnectionEnd, end } };
                return data;
            });

            var results = from client in clients from data in datas select client.QueryAsync(data);
            return Task.WhenAll(results);
        }

        private int SplitNumber(int total, int index, int agents)
        {
            int baseNumber = total / agents;
            if (index < total % agents)
            {
                baseNumber++;
            }
            return baseNumber;
        }

        private (int, int) GetConnectionRange(int total, int index, int agents)
        {
            var begin = 0;
            for (var i = 0; i < index; i++)
            {
                begin += SplitNumber(total, i, agents);
            }

            var end = begin + SplitNumber(total, index, agents);

            return (begin, end);
        }
    }
}
