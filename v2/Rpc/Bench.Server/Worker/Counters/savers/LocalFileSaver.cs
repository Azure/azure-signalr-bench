using Bench.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Bench.RpcSlave.Worker.Savers
{
    class LocalFileSaver : ISaver
    {
        public void Save(string url, long timestamp, ConcurrentDictionary<string, int> counters)
        {
            JObject jCounters = JObject.FromObject(counters);

            jCounters = Util.Sort(jCounters);

            var totalReceive = 0;
            foreach (var c in counters)
            {
                if (c.Key.Contains("message") && (c.Key.Contains(":ge") || c.Key.Contains(":lt")))
                {
                    totalReceive += c.Value;
                }
            }

            JObject rec = new JObject
            {
                { "Time", Util.Timestamp2DateTimeStr(timestamp) },
                { "Counters", jCounters },
                { "totalReceivedOnServer", counters["server:received"]},
                {"totalSent", counters["message:sent"]},
                {"totalReceive", totalReceive }
            };
            string oneLineRecord = Regex.Replace(rec.ToString(), @"\s+", "");
            oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "") + Environment.NewLine;
            Util.SaveContentToFile(url, oneLineRecord, true);
        }
    }
}
