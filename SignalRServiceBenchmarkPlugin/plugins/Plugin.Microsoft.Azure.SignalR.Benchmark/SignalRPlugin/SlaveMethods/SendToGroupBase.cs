using Common;
using Plugin.Base;
using Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods.Statistics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    public abstract class SendToGroupBase : BaseContinuousSendMethod, ISlaveMethod
    {
        // Step parameters
        protected int GroupCount;
        protected int GroupLevelRemainderBegin;
        protected int GroupLevelRemainderEnd;
        protected int GroupInternalRemainderBegin;
        protected int GroupInternalRemainderEnd;
        protected int GroupInternalModulo;
        protected int TotalConnection;
        protected SignalREnums.GroupConfigMode Mode;

        // Key
        protected static readonly string _isIngroup = "IsInGroup";

        protected class Package
        {
            public int LocalIndex;
            public IHubConnectionAdapter Connection;
            public string GroupName;
            public Dictionary<string, object> Data;
        }

        public async Task<IDictionary<string, object>> Do(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            try
            {
                Log.Information($"Send to group...");

                // Load parameters
                LoadParametersAndContext(stepParameters, pluginParameters);

                if (GroupCount == 0)
                {
                    var message = $"Group count cannot be 0";
                    Log.Error(message);
                    throw new Exception(message);
                }

                if (TotalConnection % GroupCount != 0)
                {
                    var groupMember = GroupCount != 0 ? TotalConnection / GroupCount : 0;
                    Log.Warning($"Total {TotalConnection} connections cannot be divided by group count {GroupCount}, the number of members in a group may be different from {groupMember}, may be {groupMember - 1} or {groupMember + 1}");
                }

                var sendingStep = RemainderEnd == 0 ? GroupInternalRemainderEnd : RemainderEnd;
                // Reset counters
                UpdateStatistics(StatisticsCollector, sendingStep);

                // Generate data
                var packages = GenerateData();
                // Send messages
                await SendMessages(packages);
                Log.Information($"Finish sending message {sendingStep}");
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

        protected override void LoadParametersAndContext(
            IDictionary<string, object> stepParameters,
            IDictionary<string, object> pluginParameters)
        {
            // Get parameters
            base.LoadParametersAndContext(stepParameters, pluginParameters);
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

        protected virtual IEnumerable<Package> GenerateData()
        {
            // Generate necessary data
            var messageBlob = SignalRUtils.GenerateRandomData(MessageSize);

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
                                   { SignalRConstants.MessageBlob, messageBlob },
                                   { SignalRConstants.GroupName, groupName }
                               }
                           };
            return packages;
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