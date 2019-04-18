cd $RootFolder

dotnet run -- --PidFile='./pid/pid_'$result_root'.txt' --step=UpdateServerUrl \
--PublicIps=$PublicIps \
--JobConfigFileV2=$JobConfig