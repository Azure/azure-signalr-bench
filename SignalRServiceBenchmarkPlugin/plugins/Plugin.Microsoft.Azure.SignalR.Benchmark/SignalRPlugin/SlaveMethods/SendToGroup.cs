﻿using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public class SendToGroup : BaseContinuousSendMethod, ISlaveMethod
    {
        private StatisticsCollector _statisticsCollector;

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send to group...");

                // Get parameters
                stepParameters.TryGetTypedValue(SignalRConstants.Type, out string type, Convert.ToString);
                stepParameters.TryGetTypedValue(SignalRConstants.Duration, out long duration, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.Interval, out long interval, Convert.ToInt64);
                stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out int messageSize, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out int totalConnection, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out int groupCount, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderBegin, out int GroupLevelRemainderBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderEnd, out int GroupLevelRemainderEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderBegin, out int GroupInternalRemainderBegin, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderEnd, out int GroupInternalRemainderEnd, Convert.ToInt32);
                stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalModulo, out int GroupInternalModulo, Convert.ToInt32);

                if (totalConnection % groupCount != 0) throw new Exception("Not supported: Total connections cannot be divided by group count");

                // Get context
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out IList<HubConnection> connections, (obj) => (IList<HubConnection>)obj);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionOffset}.{type}", out int offset, Convert.ToInt32);
                pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out _statisticsCollector, obj => (StatisticsCollector) obj);

                // Set callback
                SetCallback(connections);

                // Generate necessary data
                var messageBlob = new byte[messageSize];
                
                var packages = from i in Enumerable.Range(0, connections.Count)
                               let groupName = SignalRUtils.GroupName(type, i % groupCount)
                               select new
                               {
                                   Index = i,
                                   Connection = connections[i],
                                   GroupName = groupName,
                                   Data = new Dictionary<string, object>
                                   {
                                       { SignalRConstants.MessageBlob, messageBlob }, // message payload
                                       { SignalRConstants.GroupName, groupName}
                                   }
                               };

                Func<int, int, int, int, bool> IsSending = (index, modulo, beg, end) => (index % modulo) >= beg && (index % modulo) < end;

                // Send messages
                await Task.WhenAll(from package in packages
                                   let connectionIndex = package.Index + offset
                                   let groupSize = totalConnection / groupCount
                                   let groupIndex = connectionIndex % groupCount
                                   let indexInGroup = connectionIndex / groupCount
                                   let connection = package.Connection
                                   let data = package.Data
                                   where IsSending(indexInGroup, GroupInternalModulo, GroupInternalRemainderBegin, GroupInternalRemainderEnd) &&
                                         IsSending(groupIndex, groupCount, GroupLevelRemainderBegin, GroupLevelRemainderEnd)
                                   select ContinuousSend(connection, data, SendGroup,
                                        TimeSpan.FromMilliseconds(duration), TimeSpan.FromMilliseconds(interval),
                                        TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(interval)));

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to send to group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        private void SetCallback(IList<HubConnection> connections)
        {
            foreach (var connection in connections)
            {
                connection.On(SignalRConstants.SendToGroupCallbackName, (IDictionary<string, object> data) =>
                {
                    var receiveTimestamp = Util.Timestamp();
                    data.TryGetTypedValue(SignalRConstants.Timestamp, out long sendTimestamp, Convert.ToInt64);
                    var latency = receiveTimestamp - sendTimestamp;
                    _statisticsCollector.RecordLatency(latency);
                });
            }
        }

        private async Task SendGroup(HubConnection connection, IDictionary<string, object> data)
        {
            try
            {
                // Extract data
                data.TryGetTypedValue(SignalRConstants.GroupName, out string groupName, Convert.ToString);
                data.TryGetValue(SignalRConstants.MessageBlob, out var messageBlob);

                // Generate payload
                var payload = new Dictionary<string, object>
                {
                    { SignalRConstants.MessageBlob, messageBlob },
                    { SignalRConstants.Timestamp, Util.Timestamp() },
                    { SignalRConstants.GroupName, groupName }
                };

                // Send message
                await connection.SendAsync(SignalRConstants.SendToGroupCallbackName, payload);

                // Update statistics
                _statisticsCollector.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                var message = $"Error in send to group: {ex}";
                Log.Error(message);
                throw;
            }
        }
    }
}