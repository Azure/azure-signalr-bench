import BenchmarkConfigurationStep

module_name = 'Plugin.Microsoft.Azure.SignalR.Benchmark.SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark'
hub_url = "http://wanleastusagents953034586AppSvrDNS0.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS1.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS2.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS3.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS4.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS5.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS6.eastus.cloudapp.azure.com:5050/signalrbench,http://wanleastusagents953034586AppSvrDNS7.eastus.cloudapp.azure.com:5050/signalrbench"

Config = {
    BenchmarkConfigurationStep.Key['Types']: None,
    BenchmarkConfigurationStep.Key['ModuleName']: None,
    BenchmarkConfigurationStep.Key['Pipeline']: None
}


def set_config(types, pipeline):
    config = dict(Config)
    config[BenchmarkConfigurationStep.Key['Types']] = types
    config[BenchmarkConfigurationStep.Key['ModuleName']] = module_name
    config[BenchmarkConfigurationStep.Key['Pipeline']] = pipeline
    return config
