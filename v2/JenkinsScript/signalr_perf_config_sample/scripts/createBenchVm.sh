export result_root=`date +%Y%m%d%H%M%S`
RootFolder=/home/wanl/azure-signalr-bench/v2/JenkinsScript
cd $RootFolder
BenchConfig=/home/wanl/azure-signalr-bench/v2/JenkinsScript/signalr_perf_config_sample/config/bench.yaml
dotnet run -- --step=CreateBenchServer \
--PidFile='./pid/pid_'$result_root'.txt' \
--AgentConfigFile=$BenchConfig \
--DisableRandomSuffix \
--ServicePrincipal=/home/wanl/azure-signalr-bench/v2/JenkinsScript/signalr_perf_config_sample/scripts/servicePrincipal.yaml
