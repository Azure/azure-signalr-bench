#functions
start_appserver() {
  echo 'Start app server'
  export Azure__SignalR__ConnectionString=$azure_signalr_connection_string
  cd $project_root/SignalRServiceBenchmarkPlugin/utils/AppServer
  dotnet build
  dotnet run -- --urls=http://*:5050 > appserver.log
}

start_slaves() {
  echo 'Start slave'
  cd $project_root/SignalRServiceBenchmarkPlugin/framework/slave/
  dotnet build
  dotnet run -- --HostName 0.0.0.0 --RpcPort 5555
}

start_master() {
  sleep 10
  echo 'Start master'
  cd $project_root/SignalRServiceBenchmarkPlugin/framework/master/
  dotnet build
  dotnet run -- --BenchmarkConfiguration=$benchmark --SlaveList=localhost:5555
}

generate_report() {
  echo 'Generate report'
  cd $current_dir
  report_simple_dist_linux/report_simple/report_simple $project_root/SignalRServiceBenchmarkPlugin/framework/master/counters_oneline.txt
  echo 'Report saved as ./report.svg'
}

# Project root 
current_dir=`pwd`
project_root=$1
benchmark=$2
azure_signalr_connection_string=$3

# generate rpc protocol
cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc
. ./util.sh
generate_proto

# run benchmark
killall dotnet

# export useLocalSignalR=true
start_appserver & start_slaves & \
start_master; killall dotnet; generate_report
