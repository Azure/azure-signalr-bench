#functions
start_appserver() {
  echo 'Start app server'
  cd $project_root/SignalRServiceBenchmarkPlugin/utils/AppServer
  dotnet run -- --urls=http://*:5050 > appserver.log
}

start_slaves() {
  echo 'Start slave'
  cd $project_root/SignalRServiceBenchmarkPlugin/framework/slave/
  dotnet run -- --HostName 0.0.0.0 --RpcPort 5555
}

start_master() {
  sleep 10
  echo 'Start master'
  cd $project_root/SignalRServiceBenchmarkPlugin/framework/master/
  dotnet run -- --BenchmarkConfiguration=BenchmarkConfigurationSample_oneline_run.yaml --SlaveList=localhost:5555
}

generate_report() {
  echo 'Generate report'
  cd $current_dir
  python report_simple.py $project_root/SignalRServiceBenchmarkPlugin/framework/master/counters_oneline.txt
  echo 'Report saved as ./report.svg'
}

# Project root 
current_dir=`pwd`
project_root=$1
azure_signalr_connection_string=$2

# generate rpc protocol
cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc
. ./util.sh
generate_proto

# run benchmark
killall dotnet

# export useLocalSignalR=true
export Azure__SignalR__ConnectionString=$azure_signalr_connection_string
start_appserver & start_slaves & \
start_master; killall dotnet; generate_report
