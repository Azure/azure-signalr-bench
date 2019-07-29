#! /bin/bash

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
  cd $project_root/SignalRServiceBenchmarkPlugin/framework/agent/
  dotnet build
  dotnet run -- --HostName 0.0.0.0 --RpcPort 5555
}

start_master() {
  sleep 10
  echo 'Start master'
  project_master=$project_root"/SignalRServiceBenchmarkPlugin/framework/master"
  cd $project_master
  dotnet build 
  dotnet run -- --BenchmarkConfiguration=$benchmark --AgentList=localhost:5555
  cp counters.txt $current_dir"/counters.txt"
}

generate_report() {
  echo 'Generate report'
  cd $current_dir
  report_simple_dist_linux/report_simple/report_simple $current_dir"/counters.txt"
  echo 'Report saved as ./report.svg'
}

# Project root 
current_dir=`pwd`
project_root=$current_dir"/.."
azure_signalr_connection_string=$2
benchmark=$1


if [[ $benchmark != /* ]]; then
  benchmark=$current_dir"/"$benchmark
fi

# generate rpc protocol
cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc
. ./util.sh
generate_proto

# run benchmark
killall dotnet
start_appserver & start_slaves & \
start_master; killall dotnet; generate_report
