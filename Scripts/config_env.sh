#!/bin/bash

## set the SignalR bench configuration ##
if [ "$bench_name_list" == "" ]
then
  bench_name_list="echo" #"broadcast" #"echo broadcast"
fi

if [ "$bench_type_list" == "" ]
then
  bench_type_list="service" #"selfhost service"
fi

if [ "$bench_codec_list" == "" ]
then
  bench_codec_list="json" #"json msgpack"
fi

if [ "$bench_config_hub" == "" ]
then
  bench_config_hub="chat"
fi

if [ "$bench_send_size" == "" ]
then
  bench_send_size=0
fi

if [ "$use_https" == "" ]
then
  use_https=1
fi

if [ "$sigbench_run_duration" == "" ]
then
  sigbench_run_duration=240 #second running for benchmark
fi

if [ "$bench_type" == "service" ]
then
	bench_config_endpoint=${bench_service_server}:${bench_service_port}
else
	bench_config_endpoint=${bench_app_pub_server}:${bench_app_port}
fi

# check Jenkins' builtin variables
if [ "$NODE_NAME" != "" ] && [ "$JOB_NAME" != "" ]
then
  # replace the $sigbench_run_duration, $use_https and $bench_config_hub
  if [ "$__jenkins_env__" == "" ]
  then
     . ./jenkins_env.sh
  fi
fi
