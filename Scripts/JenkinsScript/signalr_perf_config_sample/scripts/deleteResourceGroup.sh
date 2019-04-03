export result_root=`date +%Y%m%d%H%M%S`

cd $RootFolder

dotnet run -- \
--PidFile='./pid/pid_'$result_root'.txt' \
--step=DeleteResourceGroupByConfig \
--AgentConfigFile=$AgentConfig \
--DisableRandomSuffix \
--ServicePrincipal=$ServicePrincipal


