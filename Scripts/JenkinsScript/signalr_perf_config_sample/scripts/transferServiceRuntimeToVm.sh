cd $RootFolder

dotnet run -- \
--step=TransferServiceRuntimeToVm \
--PidFile='./pid/pid_'$result_root'.txt'  \
--PrivateIps=$PrivateIps\
--AgentConfigFile=$AgentConfig