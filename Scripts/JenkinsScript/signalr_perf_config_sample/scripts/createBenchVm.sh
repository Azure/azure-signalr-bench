user=perftest
export result_root=`date +%Y%m%d%H%M%S`
RootFolder=/home/$user/azure-signalr-bench/v2/JenkinsScript
cd $RootFolder
BenchConfig=/home/$user/azure-signalr-bench/v2/JenkinsScript/signalr_perf_config_sample/config/bench2.yaml
dotnet run -- --step=CreateBenchServer \
--PidFile='./pid/pid_'$result_root'.txt' \
--AgentConfigFile=$BenchConfig \
--DisableRandomSuffix \
--ServicePrincipal=/home/$user/secrets/serviceprincipal.yaml
