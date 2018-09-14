export result_root=`date +%Y%m%d%H%M%S`

cd $RootFolder

dotnet run -- --step=CreateBenchServer \
--PidFile='./pid/pid_'$result_root'.txt' \
--AgentConfigFile=$BenchConfig \
--DisableRandomSuffix \
--ServicePrincipal=$ServicePrincipal