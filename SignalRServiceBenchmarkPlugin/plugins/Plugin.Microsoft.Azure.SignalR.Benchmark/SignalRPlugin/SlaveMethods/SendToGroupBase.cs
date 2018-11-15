using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Common;
using Plugin.Base;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark.SlaveMethods
{
    //public class SendToGroupBase : ISlaveMethod
    //{
    //    protected string type;
    //    protected long duration;
    //    protected long interval;
    //    protected int messageSize;
    //    protected int totalConnection;
    //    protected int groupCount;
    //    protected int GroupLevelRemainderBegin;
    //    protected int GroupLevelRemainderEnd;
    //    protected int GroupInternalRemainderBegin;
    //    protected int GroupInternalRemainderEnd;
    //    protected int GroupInternalModulo;

    //    public Task<IDictionary<string, object>> Do(IDictionary<string, object> stepParameters, IDictionary<string, object> pluginParameters)
    //    {
    //        try
    //        {
    //            Log.Information($"Send to group...");

    //        }
    //        catch (Exception ex)
    //        {
    //            var message = $"Fail to send to group: {ex}";
    //            Log.Error(message);
    //            throw;
    //        }
    //    }

    //    protected virtual void LoadParameters(IDictionary<string, object> stepParameters)
    //    {
    //        // Get parameters
    //        stepParameters.TryGetTypedValue(SignalRConstants.Type, out type, Convert.ToString);
    //        stepParameters.TryGetTypedValue(SignalRConstants.Duration, out duration, Convert.ToInt64);
    //        stepParameters.TryGetTypedValue(SignalRConstants.Interval, out interval, Convert.ToInt64);
    //        stepParameters.TryGetTypedValue(SignalRConstants.MessageSize, out messageSize, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.ConnectionTotal, out totalConnection, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupCount, out groupCount, Convert.ToInt32);

    //        // Group Mode
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderBegin, out GroupLevelRemainderBegin, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupLevelRemainderEnd, out GroupLevelRemainderEnd, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderBegin, out GroupInternalRemainderBegin, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalRemainderEnd, out GroupInternalRemainderEnd, Convert.ToInt32);
    //        stepParameters.TryGetTypedValue(SignalRConstants.GroupInternalModulo, out GroupInternalModulo, Convert.ToInt32);
    //    }
    //}
}