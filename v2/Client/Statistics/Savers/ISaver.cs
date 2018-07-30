using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using Client.Statistics;

namespace Client.Statistics.Savers
{
    public interface ISaver
    {
        void Save(string url, long timestamp, ConcurrentDictionary<string, int> counters);
    }
}
