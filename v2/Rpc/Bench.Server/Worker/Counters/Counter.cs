using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bench.Common;
using Bench.RpcSlave.Worker.Savers;

namespace Bench.RpcSlave.Worker.Counters
{
    public class Counter
    {
        private ConcurrentDictionary<string, ulong> InnerCounters { get; set; }
        public int LatencyStep { get; set; }
        public int LatencyLength { get; set; }
        private ISaver _counterSaver;

        public Counter(ISaver saver, int latencyStep = 100, int latencyLength = 10)
        {
            LatencyStep = latencyStep;
            LatencyLength = latencyLength;
            _counterSaver = saver;
            InnerCounters = new ConcurrentDictionary<string, ulong>();
            ResetCounters();
        }

        public List<Tuple<string, ulong>> GetAll()
        {
            var list = new List<Tuple<string, ulong>>();
            lock(InnerCounters)
            {
                foreach (var counter in InnerCounters)
                {
                    list.Add(new Tuple<string, ulong>(counter.Key, counter.Value));
                }
            }

            return list;
        }

        public void ResetCounters(bool withConnection = true)
        {

            // messages
            for (int i = 1; i <= LatencyLength; i++)
            {
                InnerCounters.AddOrUpdate(MsgKey(i * LatencyStep), 0, (k, v) => 0);
            }
            InnerCounters.AddOrUpdate("server:received", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate("message:notSentFromClient", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate("message:sent", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate($"message:ge:{LatencyLength * LatencyStep}", 0, (k, v) => 0);

            // connections
            if (withConnection == true)
            {
                InnerCounters.AddOrUpdate("connection:error", 0, (k, v) => 0);
                InnerCounters.AddOrUpdate("connection:success", 0, (k, v) => 0);
            }

            // join/leave groups
            InnerCounters.AddOrUpdate("group:join:fail", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate("group:leave:fail", 0, (k, v) => 0);

            // message size
            InnerCounters.AddOrUpdate("message:sendSize", 0, (k, v) => 0);
            InnerCounters.AddOrUpdate("message:recvSize", 0, (k, v) => 0);
        }

        public void IncreaseJoinGroupFail()
        {
            InnerCounters.AddOrUpdate("group:join:fail", 0, (k, v) => v + 1);

        }

        public void IncreaseLeaveGroupFail()
        {
            InnerCounters.AddOrUpdate("group:leave:fail", 0, (k, v) => v + 1);

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

            InnerCounters.AddOrUpdate($"message:ge:{LatencyLength * LatencyStep}", 0, (k, v) => v + 1);
        }

        public void IncreaseSentMessageSize(ulong size)
        {
            InnerCounters.AddOrUpdate("message:sendSize", 0, (k, v) => v + size);
        }

        public void IncreaseReceivedMessageSize(ulong size)
        {
            InnerCounters.AddOrUpdate("message:recvSize", 0, (k, v) => v + size);
        }

        public void IncreseSentMsg()
        {
            InnerCounters.AddOrUpdate("message:sent", 0, (k, v) => v + 1);
        }

        public void IncreaseConnectionError()
        {
            InnerCounters.AddOrUpdate("connection:error", 0, (k, v) => v + 1);

        }

        public void UpdateConnectionSuccess(ulong totalConn)
        {
            InnerCounters.AddOrUpdate("connection:success", 0, (k, v) =>
            {
                InnerCounters.TryGetValue("connection:error", out ulong errConn);
                return totalConn - errConn;
            });
        }

        public void IncreseNotSentFromClientMsg()
        {
            InnerCounters.AddOrUpdate("message:notSentFromClient", 0, (k, v) => v + 1);
        }

        public void SetServerCounter(ulong count)
        {
            InnerCounters.AddOrUpdate("server:received", count, (k, v) => Math.Max(count, v));
        }

        private string MsgKey(int latency)
        {
            return $"message:lt:{latency}";
        }

        public void SaveCounters()
        {
            // TODO: choose lightest lock
            lock(InnerCounters)
            {
                _counterSaver.Save("Record.txt", Util.Timestamp(), InnerCounters);
            }
        }
    }

}