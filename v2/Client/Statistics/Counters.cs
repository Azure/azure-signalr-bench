using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Client.Statistics.Savers;
using Client.UtilNs;

namespace Client.Statistics
{
    public class Counters: ICounters
    {
        public ConcurrentDictionary<string, int> InnerCounters { get; set; }
        public int LatencyStep { get; set; }
        public int LatencyLength { get; set; }
        private ISaver _counterSaver;


        public Counters(ISaver saver, int latencyStep=100, int latencyLength=10)
        {
            LatencyStep = latencyStep;
            LatencyLength = latencyLength;
            _counterSaver = saver;
            InnerCounters = new ConcurrentDictionary<string, int>();
            ResetCounters();
        }

        public ConcurrentDictionary<string, int> GetAll()
        {
            return InnerCounters;
        }

        public void ResetCounters()
        {
            for (int i = 1; i <= LatencyLength; i++)
            {
                InnerCounters.AddOrUpdate(MsgKey(i * LatencyStep), 0, (k, v) => 0);
            }
            InnerCounters.AddOrUpdate("message:send", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate("message:invalid", 0, (k, v) => 0);
        }

        public void CountLatency(long sendTimestamp, long receiveTimestamp)
        {
            long dTime = receiveTimestamp - sendTimestamp;
            for (int j = 1; j <= LatencyLength; j++)
            {
                if (dTime < j * LatencyStep)
                {
                    InnerCounters.AddOrUpdate(MsgKey(j * LatencyStep), 0, (k, v) => v + 1);
                    return;
                }
            }

            InnerCounters.AddOrUpdate("message:invalid", 0, (k, v) => v + 1);
        }

        public void IncreseSentMsg()
        {
            InnerCounters.AddOrUpdate("message:send", 0, (k, v) => v + 1);
        }

        private string MsgKey(int latency)
        {
            return $"message:lt:{latency}";
        }

        public void SaveCounters()
        {
            // TODO: choose lightest lock
            lock (InnerCounters)
            {
                _counterSaver.Save("Record.txt", Util.Timestamp(), InnerCounters);
            }
        }
    }

    
}
