﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        protected string Type;
        protected long Duration;
        protected long Interval;
        protected int MessageSize;
        protected int TotalConnection;
        protected int GroupCount;
        protected int GroupLevelRemainderBegin;
        protected int GroupLevelRemainderEnd;
        protected int GroupInternalRemainderBegin;
        protected int GroupInternalRemainderEnd;
        protected int GroupInternalModulo;
        protected int RemainderBegin;
        protected int RemainderEnd;
        protected int Modulo;

        protected SignalREnums.GroupConfigMode Mode;

        // Context
        protected IList<HubConnection> Connections;
        protected StatisticsCollector StatisticsCollector;
        protected List<int> ConnectionIndex;
        protected List<SignalREnums.ConnectionState> ConnectionsSuccessFlag;

        // Key
        protected static readonly string _isIngroup = "IsInGroup";

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
                if (TotalConnection % GroupCount != 0) throw new Exception("Not supported: Total connections cannot be divided by group count");

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
            SignalRUtils.ResetCounters(StatisticsCollector);
        }

        protected virtual void LoadParameters(IDictionary<string, object> stepParameters)
        {
            // Get parameters
            stepParameters.TryGetTypedValue(SignalRConstants.Type, out Type, Convert.ToString);
            stepParameters.TryGetTypedValue(SignalRConstants.Duration, out Duration, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.Interval, out Interval, Convert.ToInt64);
            stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out MessageSize, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out TotalConnection, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out GroupCount, Convert.ToInt32);
            stepParameters.TryGetTypedValue(SignalRConstants.GroupConfigMode, out string groupConfigMode, Convert.ToString);

            var success = Enum.TryParse(groupConfigMode, out Mode);

            if (!success)
            {
                var message = $"Config mode not supported: {groupConfigMode}";
                Log.Error(message);
                throw new Exception(message);
            }

            switch (Mode)
            {
                case SignalREnums.GroupConfigMode.Group:
                    stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderBegin, out GroupLevelRemainderBegin, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderEnd, out GroupLevelRemainderEnd, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderBegin, out GroupInternalRemainderBegin, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderEnd, out GroupInternalRemainderEnd, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalModulo, out GroupInternalModulo, Convert.ToInt32);
                    break;
                case SignalREnums.GroupConfigMode.Connection:
                    stepParameters.TryGetTypedValue(SignalRConstants.RemainderBegin, out RemainderBegin, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.RemainderEnd, out RemainderEnd, Convert.ToInt32);
                    stepParameters.TryGetTypedValue(SignalRConstants.Modulo, out Modulo, Convert.ToInt32);
                    break;
            }
        }

        protected virtual void LoadContext(IDictionary<string, object> pluginParameters)
        {
            // Get context
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionStore}.{Type}", out Connections, (obj) => (IList<HubConnection>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.StatisticsStore}.{Type}", out StatisticsCollector, obj => (StatisticsCollector)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionIndex}.{Type}", out ConnectionIndex, (obj) => (List<int>)obj);
            pluginParameters.TryGetTypedValue($"{SignalRConstants.ConnectionSuccessFlag}.{Type}", out ConnectionsSuccessFlag, (obj) => (List<SignalREnums.ConnectionState>)obj);
        }

        protected virtual IEnumerable<Package> GenerateData()
        {
            // Generate necessary data
            var messageBlob = new byte[MessageSize];

            var packages = from i in Enumerable.Range(0, Connections.Count)
                           let groupName = SignalRUtils.GroupName(Type, i % GroupCount)
                           select 
                           new Package
                           {
                               LocalIndex = i,
                               Connection = Connections[i],
                               GroupName = groupName,
                               Data = new Dictionary<string, object>
                                   {
                                       { SignalRConstants.MessageBlob, messageBlob }, // message payload
                                       { SignalRConstants.GroupName, groupName}
                                  }
                           };

            return packages;
        }

        protected virtual async Task SendGroup(int localIndex, IDictionary<string, object> data)
        {
            try
            {
                // Is the connection is not active, then stop sending message
                if (ConnectionsSuccessFlag[localIndex] != SignalREnums.ConnectionState.Success) return;

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
                await Connections[localIndex].SendAsync(SignalRConstants.SendToGroupCallbackName, payload);

                // Update statistics
                StatisticsCollector.IncreaseSentMessage();
            }
            catch (Exception ex)
            {
                ConnectionsSuccessFlag[localIndex] = SignalREnums.ConnectionState.Fail;
                var message = $"Error in send to group: {ex}";
                Log.Error(message);
                //throw;
            }
        }

        protected virtual async Task JoinLeaveGroup(int localIndex, IDictionary<string, object> data)
        {
            // Extract data
            data.TryGetTypedValue(SignalRConstants.GroupName, out string groupName, Convert.ToString);
            data.TryGetTypedValue(_isIngroup, out bool isInGroup, Convert.ToBoolean);

            // Join or leave groups
            if (isInGroup)
            {
                try
                {
                    await Connections[localIndex].SendAsync(SignalRConstants.LeaveGroupCallbackName, groupName);
                }
                catch
                {
                    StatisticsCollector.IncreaseLeaveGroupFail();
                }
            }
            else
            {
                try
                {
                    await Connections[localIndex].SendAsync(SignalRConstants.JoinGroupCallbackName, groupName);
                }
                catch
                {
                    StatisticsCollector.IncreaseJoinGroupFail();
                }
            }

            data[_isIngroup] = !isInGroup;
        }
    }
}