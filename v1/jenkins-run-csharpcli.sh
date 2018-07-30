#!/bin/bash
. ./jenkins_env.sh

MaxConnectionNumber=100000

if [ "$connection_string" == "" ]
then
   echo "connection string must not be empty!"
   exit 1
fi

if [ "$connection_number" == "" ]
then
   echo "connection number must not be empty"
   exit 1
fi

if [ $connection_number -gt $MaxConnectionNumber ]
then
   echo "connection number's maximum limit is $MaxConnectionNumber"
   exit 1
fi

if [ $connection_number -lt 1 ]
then
   echo "connection number's minimum limit is 1"
   exit 1
fi

if [ "$send_number" == "" ]
then
   echo "Warning: send number is empty, so the default send number will be set to connection number"
   send_number=$connection_number
fi

if [ $send_number -gt $connection_number ]
then
   echo "Warning: currently we did not support sending number larger than connection number, so set it to be connection number"
   send_number=$connection_number
fi
# jenkins normalized input

echo "-------jenkins normalize your inputs------"
echo "[ConnectionString]: $connection_string"
echo "[ClientConnectionNumber]: $connection_number"
echo "[ConcurrentConnectionNumber]: $connection_concurrent"
echo "[SendNumber]: $send_number"
echo "[Duration]: $sigbench_run_duration"

. ./csharpcli.sh

service_name=$(extract_servicename_from_connectionstring $connection_string)
app_launch_log_file=""
k8s_result_dir=""
if [ -d $result_root/$bench_type_list ]
then
  app_launch_log_file=$result_root/${bench_type_list}/${app_running_log}
  k8s_result_dir=$result_root/$bench_type_list
else
  app_launch_log_file=${result_dir}/${app_running_log}
  k8s_result_dir=$result_root
fi

start_cli_bench_server $connection_string $app_launch_log_file

err_check=`grep -i "error" ${app_launch_log_file}`
if [ "$err_check" != "" ]
then
   echo "Fail to start app server: $err_check"
   cat ${app_launch_log_file}
   exit 1
fi

. ./kubectl_utils.sh

if [ "$service_name" != "" ]
then
   nohup sh collect_connections.sh $service_name $k8s_result_dir &
   collect_conn_pid=$!
else
   echo "It seems you are running a self-host SignalR service because the service name is not standard"
fi

sh run_csharp_cli.sh

echo "Stop app server"
stop_cli_bench_server

if [ "$service_name" != "" ]
then
   kill $collect_conn_pid
   if [ "$copy_syslog" == "true" ]
   then
     copy_syslog $service_name $k8s_result_dir
   fi
   get_k8s_pod_status $service_name $k8s_result_dir
fi

sh gen_html.sh $connection_string # gen_html
