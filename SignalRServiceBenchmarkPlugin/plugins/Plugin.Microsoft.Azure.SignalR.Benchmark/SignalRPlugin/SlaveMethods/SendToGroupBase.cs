using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR.Client;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public abstract class SendToGroupBase : BaseContinuousSendMethod, ISlaveMethod
    {
        // Step parameters
        protected string type;
        protected long duration;
        protected long interval;
        protected int messageSize;
        protected int totalConnection;
        protected int groupCount;
        protected int GroupLevelRemainderBegin;
        protected int GroupLevelRemainderEnd;
        protected int GroupInternalRemainderBegin;
        protected int GroupInternalRemainderEnd;
        protected int GroupInternalModulo;

        // Context
        protected IList<HubConnection> connections;
        protected StatisticsCollector statisticsCollector;
        protected List<int> connectionIndex;
        protected List<SignalREnums.ConnectionState> connectionsSuccessFlag;

        // Key
        private static readonly string _isIngroup = "IsInGroup";

        protected class Package
        {
            public int LocalIndex;
            public HubConnection Connection;
            public string GroupName;
            public Dictionary<string, object> Data;
        }

        public async Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send to group...");

                // Load parameters
                LoadParameters(stepParameters);
                if (totalConnection % groupCount != 0) throw new Exception("Not supported: Total connections cannot be divided by group count");

                // Load context
                LoadContext(pluginParameters);

                // Generate data
                var packages = GenerateData();

                // Reset counters
                ResetCounters();

                // Send messages
                await SendMessages(packages);

                return null;
            }
            catch (Exception ex)
            {
                var message = $"Fail to send to group: {ex}";
                Log.Error(message);
                throw;
            }
        }

        protected abstract Task SendMessages(IEnumerable<Package> packages);

        protected virtual bool IsSending(int index, int modulo, int beg, int end) => (index % modulo) >= beg && (index % modulo) < end;

        protected virtual void ResetCounters()
        {
            statisticsCollector.ResetGroupCounters();
            statisticsCollector.ResetMessageCounters();
        }

        protected virtual void LoadParameters(IDictionary<string, object> stepParameters)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out duration, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out messageSize, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out totalConnection, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out groupCount, Convert.ToInt32);

            // Group Mode
            stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderBegin, out GroupLevelRemainderBegin, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderEnd, out GroupLevelRemainderEnd, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderBegin, out GroupInternalRemainderBegin, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderEnd, out GroupInternalRemainderEnd, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalModulo, out GroupInternalModulo, Convert.ToInt32);
        }

        protected virtual void LoadContext(IDictionary<string, object> pluginParameters)
        {
            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{type}", out connections, (obj) => (IList<HubConnection>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{type}", out statisticsCollector, obj => (StatisticsCollector)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{type}", out connectionIndex, (obj) => (List<int>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{type}", out connectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);
        }

        protected IEnumerable<Package> GenerateData()
        {
            // Generate necessary data
            var messageBlob = new byte[messageSize];

            var packages = from i in Enumerable.Range(0, connections.Count)
                           let groupName = SignalRUtils.GroupName(type, i % groupCount)
                           select 
                           new Package
                           {
                               LocalIndex = i,
                               Connection = connections[i],
                               GroupName = groupName,
                               Data = new Dictionary<string, object>
                                   {
                                       { SignalRConstants.MessageBlob, messageBlob }, // message payload
                                       { SignalRConstants.GroupName, groupName}
                                  }
                           };

            return packages;
        }

        protected virtual async Task SendGroup((HubConnection Connection, int LocalIndex, List<SignalREnums.ConnectionState> ConnectionsSuccessFlag, StatisticsCollector StatisticsCollector) package, IDictionary<string, object> data)
        {
            try
            {
                // Is the connection is not active, then stop sending message
                if (package.ConnectionsSuccessFlag[package.LocalIndex] != SignalREnums.ConnectionState.Success) return;

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
                await package.Connection.SendAsync(SignalRConstants.SendToGroupCallbackName, payload);

                // Update statistics
                package.StatisticsCollector.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                package.ConnectionsSuccessFlag[package.LocalIndex] = SignalREnums.ConnectionState.Fail;
                var message = $"Error in send to group: {ex}";
                Log.Error(message);
                //throw;
            }
        }

        protected virtual async Task JoinLeaveGroup((HubConnection Connection, int LocalIndex, List<SignalREnums.ConnectionState> ConnectionsSuccessFlag, StatisticsCollector StatisticsCollector) package, IDictionary<string, object> data)
        {
            // Extract data
            data.TryGetTypedValue(SignalRConstants.GroupName, out string groupName, Convert.ToString);
            data.TryGetTypedValue(_isIngroup, out bool isInGroup, Convert.ToBoolean);

            // Join or leave groups
            if (isInGroup)
            {
                try
                {
                    await package.Connection.SendAsync(SignalRConstants.LeaveGroupCallbackName, groupName);
                }
                catch
                {
                    package.StatisticsCollector.IncreaseLeaveGroupFail();
                }
            }
            else
            {
                try
                {
                    await package.Connection.SendAsync(SignalRConstants.JoinGroupCallbackName, groupName);
                }
                catch
                {
                    package.StatisticsCollector.IncreaseJoinGroupFail();
                }
            }

            data[_isIngroup] = !isInGroup;
        }
    }
}