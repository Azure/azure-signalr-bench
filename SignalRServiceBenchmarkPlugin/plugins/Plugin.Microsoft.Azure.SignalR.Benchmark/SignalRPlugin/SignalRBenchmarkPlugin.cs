using Newtonsoft.Json;
using Plugin.Base;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    // Sample plugin: SignalR benchmark
    public class SignalRBenchmarkPlugin : IPlugin
    {
        private string _masterNamespaceSuffix = "MasterMethods";
        private string _slaveNamespaceSuffix = "SlaveMethods";

        public IDictionary<string, object> PluginMasterParameters { get; set; } = new ConcurrentDictionary<string, object>();
        public IDictionary<string, object> PluginSlaveParamaters { get; set; } = new ConcurrentDictionary<string, object>();

        public IMasterMethod CreateMasterMethodInstance(string methodName)
        {
            var currentNamespace = GetType().Namespace;
            var fullMethodName = $"{currentNamespace}.{_masterNamespaceSuffix}.{methodName}, {currentNamespace}";
            var type = Type.GetType(fullMethodName);
            IMasterMethod methodInstance = (IMasterMethod)Activator.CreateInstance(type);
            return methodInstance;
        }

        public ISlaveMethod CreateSlaveMethodInstance(string methodName)
        {
            var currentNamespace = GetType().Namespace;
            var fullMethodName = $"{currentNamespace}.{_slaveNamespaceSuffix}.{methodName}, {currentNamespace}";
            var type = Type.GetType(fullMethodName);
            ISlaveMethod methodInstance = (ISlaveMethod)Activator.CreateInstance(type);
            return methodInstance;
        }

        public Dictionary<string, object> Deserialize(string input)
        {
            try
            {
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(input);
                return parameters;
            }
            catch (Exception ex)
            {
                Log.Error($"Deserialize data failed: {ex.Message}");
                return null;
            }
        }

        public string Serialize(IDictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return json;
        }
    }
}
