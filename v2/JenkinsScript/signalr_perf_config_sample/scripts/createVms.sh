export result_root=`date +%Y%m%d%H%M%S`

cd $RootFolder

dotnet run -- --step=CreateAllVmsInSameVnet \
--VnetGroupName=$BenchServerGroup \
--VnetName=$VnetName \
--SubnetName=$SubnetName \
--PidFile='./pid_'$result_root'.txt'  \
--AgentConfigFile=$AgentConfig \
--DisableRandomSuffix \
--ServicePrincipal=$ServicePrincipal

