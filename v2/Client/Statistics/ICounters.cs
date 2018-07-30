using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Client.Statistics.Savers;


namespace Client.Statistics
{
    public interface ICounters
    {
        ConcurrentDictionary<string, int> InnerCounters { get; set; }
        int LatencyStep { get; set; }
        int LatencyLength { get; set; }
        ConcurrentDictionary<string, int> GetAll();
        void ResetCounters();
        void CountLatency(long sendTimestamp, long receiveTimestamp);
        void IncreseSentMsg();
        void SaveCounters();
    }
}
