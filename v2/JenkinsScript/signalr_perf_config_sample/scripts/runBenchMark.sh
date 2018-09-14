export result_root=`date +%Y%m%d%H%M%S`

mkdir -p ~/signalr-bench-statistics-${ResultFolderSuffix}/logs/${result_root}


cd $RootFolder

dotnet run -- --PidFile='./pid/pid_'$result_root'.txt' --step=AllInSameVnet \
--branch=$Branch \
--PrivateIps=$PrivateIps \
--PublicIps=$PublicIps \
--AgentConfigFile=$AgentConfig \
--JobConfigFileV2=$JobConfig \
--sendToFixedClient=$SendToFixedClient \
--StatisticsSuffix=$ResultFolderSuffix \
--ServicePrincipal=$ServicePrincipal \
--AzureSignalrConnectionString=$AzureSignalrConnectionString > ~/signalr-bench-statistics-$ResultFolderSuffix/logs/${result_root}/log_bench.txt

