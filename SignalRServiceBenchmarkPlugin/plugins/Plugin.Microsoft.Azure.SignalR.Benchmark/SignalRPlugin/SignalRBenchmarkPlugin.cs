using Common;
using Newtonsoft.Json;
using Plugin.Base;
using Rpc.Service;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Microsoft.Azure.SignalR.Benchmark
{
    // Sample plugin: SignalR benchmark
    public class SignalRBenchmarkPlugin : IPlugin, ISignalRPlugin
    {
        private string _masterNamespaceSuffix = "MasterMethods";
        private string _slaveNamespaceSuffix = "SlaveMethods";

        private string _simpleConfigurationTemplate = @"
mode: Simple                                                                         # Required: 'Simple|Advanced', default is 'Simple'
kind: perf                                                                           # Optional: 'perf|longrun', default is 'perf'
config:
  connectionString: Endpoint=https://xxxx.signalr.net;AccessKey=xxx;Version=1.0; # Required
  webAppTarget: http://localhost:5050/signalrbench                                   # Optional: if not specified, an internal webapp is launched on http://localhost:5050, hubname is 'signalrbench'
  connections: 1000                                                                  # Optional, default is 1000
  arrivingRate: 50                                                                   # Optional, default is 50
  transport: Websockets                                                              # Optional: 'Websockets|ServerSentEvents|LongPolling', default is Websockets
  protocol: json                                                                     # Optional: 'json|messagepack' default is json
  singleStepDuration: 240000                                                         # Optional, default is 240 seconds
  baseSending: 500                                                                   # Optional, the count for active sending connections when starting, default is 500
  sendingSteps: 2                                                                    # Optional: maximum value is 'Connections / BaseSending', mimum value is 1. Default is 'Connections / BaseSending'
  connectionType: Core                                                               # Optional: 'Core|AspNet', default is 'Core', if you use AspNet SignalR, please choose 'AspNet'.
  arrivingBatchMode: HighPress                                                       # Optional: 'HighPress|LowPress', default is 'HighPress'
  arrivingBatchWait: 1000                                                            # Optional: waiting period during batch connection, default is 1000 millisecond
  connectionFailPercentage: 0.01                                                     # Optional: connection failure tolerance, default is 0.01 which means it allows at most 1% connections fail, and try to reconnect. Otherwise stop.
  latencyPercentage: 0.01                                                            # Optional: message latency tolerance, default is 0.01 which means it allows at most 1% message latency > 1s, otherwise stop
scenario:
  name: echo                                                                         # Optional: 'echo|broadcast|sendToGroup|sendToClient|restSendToUser|restSendToGroup|restBroadcast|restPersistSendToUser|restPersistSendToGroup|restPersistBroadcast', default is echo
  parameters:
    messageSize: 2048                                                                # Optional: default is 2048 bytes
    sendingInterval: 1000                                                            # Optional: default is 1000 milliseconds
    groupCount: 500                                                                  # Optional: group count only valid for 'sendToGroup|restSendToGroup|restPersistSendToGroup', ignored for other scenarios
";
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

        public void DumpConfiguration(string configuration)
        {
            var benchConfig = new BenchmarkConfiguration(configuration);
            if (benchConfig.IsSimple)
            {
                benchConfig.Dump();
                return;
            }
        }

        public async Task<string> ExecuteOnAgent(string parametersInJson)
        {
            var parameters = Deserialize(parametersInJson);

            // Display configurations
            var configuration = (from entry in parameters select $"  {entry.Key} : {entry.Value}").Aggregate((a, b) => a + Environment.NewLine + b);
            Log.Information($"Configuration:{Environment.NewLine}{configuration}");

            // Extract method name
            parameters.TryGetTypedValue(Constants.Method, out string method, Convert.ToString);

            // Create Instance
            try
            {
                var methodInstance = CreateSlaveMethodInstance(method);
                var result = await methodInstance.Do(parameters, PluginSlaveParamaters);
                if (result != null)
                {
                    return Serialize(result);
                }
                return null;
            }
            catch (Exception ex)
            {
                var message = $"Perform method '{method}' fail:{Environment.NewLine} {ex}";
                throw new Exception(message);
            }
        }

        public void Help()
        {
            Console.WriteLine(_simpleConfigurationTemplate);
        }

        public string Serialize(IDictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return json;
        }

        public async Task Start(string configurationContent, IList<IRpcClient> clients)
        {
            var benchConfig = new BenchmarkConfiguration(configurationContent);

            var stepHandler = new StepHandler(this);
            foreach (var client in clients)
            {
                client.InstallSerializerAndDeserializer(Serialize, Deserialize);
            }
            var clsName = $"{GetType().FullName}, {GetType().Namespace}";
            await InstallPluginInSlaves(clients, clsName);
            // Process pipeline
            try
            {
                foreach (var parallelStep in benchConfig.Pipeline)
                {
                    var tasks = new List<Task>();
                    foreach (var step in parallelStep)
                    {
                        tasks.Add(stepHandler.HandleStep(step, clients));
                    }
                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Stop for {e.Message}");
            }
        }

        private async Task InstallPluginInSlaves(IList<IRpcClient> clients, string moduleName)
        {
            Log.Information($"Install plugin in slaves...");

            var tasks = new List<Task<bool>>();

            // Try to install plugin
            var installResults = await Task.WhenAll(from client in clients
                                                    select client.InstallPluginAsync(moduleName));
            var success = installResults.All(result => result);

            if (!success) throw new Exception("Fail to install plugin in slaves.");
        }
    }
}
