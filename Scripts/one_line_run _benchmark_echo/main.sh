# Project root 
project_root=$1

cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc
./build.sh

# build projects
build_rpc
build_master
build_slave
build_app_server

# run automation tool
cd $project_root/SignalRServiceBenchmarkPlugin/utils/Commander
dotnet run -- \
--SlaveList=localhost:5555 \
--MasterHostname=localhost \
--AppServerHostnames=http://localhost:5050/signalrbench \
--Username=wanl \
--Password=12151215 \
--AppserverProject=$project_root/SignalRServiceBenchmarkPlugin/utils/AppServer \
--MasterProject=$project_root/SignalRServiceBenchmarkPlugin/framework/master \
--SlaveProject=$project_root/SignalRServiceBenchmarkPlugin/framework/slave \
--MasterTargetPath=$project_root/SignalRServiceBenchmarkPlugin/framework/master \
--SlaveTargetPath=$project_root/SignalRServiceBenchmarkPlugin/framework/slave \
--AppserverTargetPath=$project_root/SignalRServiceBenchmarkPlugin/utils/AppServer \
--BenchmarkConfigurationTargetPath=BenchmarkConfigurationSample.yaml


