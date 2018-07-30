using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Client.UtilNs;

namespace Client.Statistics.Savers
{
    class LocalFileSaver : ISaver
    {
        public void Save(string url, long timestamp, ConcurrentDictionary<string, int> counters)
        {
            JObject jCounters = JObject.FromObject(counters);

            jCounters = Sort(jCounters);

            var totalReceive = 0;
            foreach (var c in counters)
            {
                if (c.Key != "message:send")
                {
                    totalReceive += c.Value;
                }
            }

            JObject rec = new JObject
            {
                { "Time", Timestamp2DateTimeStr(timestamp) },
                { "Counters", jCounters },
                {"totalSend", counters["message:send"]},
                {"totalReceive", totalReceive }
            };
            string oneLineRecord = Regex.Replace(rec.ToString(), @"\s+", "");
            oneLineRecord = Regex.Replace(oneLineRecord, @"\t|\n|\r", "") + Environment.NewLine;
            SaveFile(@"Record.txt", oneLineRecord);
        }

        private void SaveFile(string path, string content)
        {
            if (!File.Exists(path))
            {
                StreamWriter sw = File.CreateText(path);
            }
            File.AppendAllText(path, content);

        }

        private string Timestamp2DateTimeStr(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(timestamp).ToString("yyyy-MM-ddThh:mm:ssZ");
        }

        private JObject Sort(JObject jobj)
        {
            Util.Log($"jobj = {jobj.ToString()}");
            var sorted = new JObject(
                jobj.Properties().OrderBy(p =>
                {
                    Util.Log($"p.name = {p.Name}");
                    var startInd = p.Name.LastIndexOf(":") + 1;
                    int latency = 99999;
                    try
                    {
                        latency = Convert.ToInt32((p.Name.Substring(startInd)));
                    }
                    catch (Exception)
                    {
                    }
                    Util.Log($"latencay = {latency}");
                    return latency;
                })
            );
            Util.Log($"sorted jobj = {sorted.ToString()}");
            return sorted;
            
        }
    }
}
