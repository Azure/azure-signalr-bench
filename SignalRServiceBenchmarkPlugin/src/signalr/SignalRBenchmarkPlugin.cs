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
        private string _agentNamespaceSuffix = "AgentMethods";
        private string _simpleConfigurationTemplate = $@"
mode: simple                                                                         # Required: '{SimpleBenchmarkModel.DEFAULT_MODE}|{SimpleBenchmarkModel.ADVANCED_MODE}', default is '{SimpleBenchmarkModel.DEFAULT_MODE}'
kind: perf                                                                           # Optional: '{SimpleBenchmarkModel.DEFAULT_KIND}|{SimpleBenchmarkModel.PARSERESULT_KIND}', default is '{SimpleBenchmarkModel.DEFAULT_KIND}'
config:
  connectionString: Endpoint=https://xxxx.signalr.net;AccessKey=xxx;Version=1.0;     # Optional: if 'webAppTarget' has specified, this option will be ignored, otherwise you must specify it.
  webAppTarget: http://localhost:5050/signalrbench                                   # Optional: if not specified, the default webapp url is assumed to be {SimpleBenchmarkModel.DEFAULT_WEBAPP_HUB_URL}
  connections: 1000                                                                  # Optional, default is {SimpleBenchmarkModel.DEFAULT_CONNECTIONS}
  arrivingRate: 50                                                                   # Optional, default is {SimpleBenchmarkModel.DEFAULT_ARRIVINGRATE}
  transport: Websockets                                                              # Optional: '{SimpleBenchmarkModel.DEFAULT_TRANSPORT}|{SimpleBenchmarkModel.SSE_TRANSPORT}|{SimpleBenchmarkModel.LONGPOLLING_TRANSPORT}', default is {SimpleBenchmarkModel.DEFAULT_TRANSPORT}
  protocol: json                                                                     # Optional: '{SimpleBenchmarkModel.DEFAULT_PROTOCOL}|{SimpleBenchmarkModel.MSGPACK_PROTOCOL}' default is {SimpleBenchmarkModel.DEFAULT_PROTOCOL}
  singleStepDuration: 240000                                                         # Optional, default is {SimpleBenchmarkModel.DEFAULT_SINGLE_STEP_DUR} mill-seconds
  baseSending: 500                                                                   # Optional, the count for active sending connections when starting, default is {SimpleBenchmarkModel.DEFAULT_BASE_SENDING_STEP}
  sendingSteps: 0                                                                    # Optional: maximum value is 'Connections / BaseSending', minimum value is 1. Default is 0, means getting from 'Connections / BaseSending'
  step: 500                                                                          # Optional: default is {SimpleBenchmarkModel.DEFAULT_STEP}
  connectionType: Core                                                               # Optional: '{SimpleBenchmarkModel.DEFAULT_CONNECTION_TYPE}|{SimpleBenchmarkModel.ASPNET_CONNECTION_TYPE}', default is '{SimpleBenchmarkModel.DEFAULT_CONNECTION_TYPE}', if you use AspNet SignalR, please choose '{SimpleBenchmarkModel.ASPNET_CONNECTION_TYPE}'.
  debug: false                                                                       # Optional: dump more details if it is true, default is false
scenario:
  name: echo                                                                         # Optional: '{SimpleBenchmarkModel.DEFAULT_SCENARIO}|broadcast|sendToGroup|sendToClient|restSendToUser|restSendToGroup|restBroadcast|restPersistSendToUser|restPersistSendToGroup|restPersistBroadcast', default is {SimpleBenchmarkModel.DEFAULT_SCENARIO}
  parameters:
    messageSize: 2048                                                                # Optional: default is {SimpleBenchmarkModel.DEFAULT_MESSAGESIZE} bytes
    sendingInterval: 1000                                                            # Optional: default is {SimpleBenchmarkModel.DEFAULT_SEND_INTERVAL} milliseconds
    groupCount: 500                                                                  # Optional: group count only valid for 'sendToGroup|restSendToGroup|restPersistSendToGroup', ignored for other scenarios
";

        /**
         * some other hidden properties:
           arrivingBatchMode: HighPress                                             # Optional: '{SimpleBenchmarkModel.DEFAULT_ARRIVING_BATCH_MODE}|{SimpleBenchmarkModel.LOW_ARRIVING_BATCH_MODE}', default is '{SimpleBenchmarkModel.DEFAULT_ARRIVING_BATCH_MODE}'
        */
        public IDictionary<string, object> PluginMasterParameters { get; set; } = new ConcurrentDictionary<string, object>();

        public IDictionary<string, object> PluginAgentParamaters { get; set; } = new ConcurrentDictionary<string, object>();

        public IMasterMethod CreateMasterMethodInstance(string methodName)
        {
            var currentNamespace = GetType().Namespace;
            var fullMethodName = $"{currentNamespace}.{_masterNamespaceSuffix}.{methodName}, {currentNamespace}";
            var type = Type.GetType(fullMethodName);
            IMasterMethod methodInstance = (IMasterMethod)Activator.CreateInstance(type);
            return methodInstance;
        }

        public IAgentMethod CreateAgentMethodInstance(string methodName)
        {
            var currentNamespace = GetType().Namespace;
            var fullMethodName = $"{currentNamespace}.{_agentNamespaceSuffix}.{methodName}, {currentNamespace}";
            var type = Type.GetType(fullMethodName);
            IAgentMethod methodInstance = (IAgentMethod)Activator.CreateInstance(type);
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
            parameters.TryGetTypedValue(SignalRConstants.Method, out string method, Convert.ToString);

            // Create Instance
            try
            {
                var methodInstance = CreateAgentMethodInstance(method);
                var result = await methodInstance.Do(parameters, PluginAgentParamaters);
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

        public bool NeedAgents(string configuration)
        {
            // Simple configuration to parse result (counters.txt) does not need slave agents
            return !BenchmarkConfiguration.IsConfigInSimpleMode(configuration) ||
                   !BenchmarkConfiguration.IsResultParser(configuration);
        }

        public string Serialize(IDictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return json;
        }

        public async Task Start(string configurationContent, IList<IRpcClient> clients)
        {
            var benchConfig = new BenchmarkConfiguration(configurationContent);
            if (benchConfig.Debug)
            {
                DumpConfiguration(configurationContent);
            }
            if (benchConfig.Pipeline.Count == 0)
            {
                if (BenchmarkConfiguration.IsResultParser(configurationContent))
                {
                    BenchmarkConfiguration.ParseResult(configurationContent);
                }
                return;
            }
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
                        tasks.Add(stepHandler.HandleStep(step, clients, benchConfig.Debug));
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
