#!/bin/bash
. ./jenkins_env.sh
. ./servers_env.sh

MaxConnectionNumber=1000000

if [ "$connection_string_list" == "" ]
then
   echo "connection string list must not be empty!"
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
# jenkins normalized input

echo "-------jenkins normalize your inputs------"
echo "[ConnectionStringList]: $connection_string_list"
echo "[ClientConnectionNumber]: $connection_number"
echo "[ConcurrentConnectionNumber]: $connection_concurrent"
echo "[SendNumber]: $send_number"
echo "[Duration]: $sigbench_run_duration"
echo "-------internal configuration------"
echo "[AppServerList]: $bench_app_pub_server (this list should have the same items as connection string list)"
echo "[AppServerLoginUser]: $bench_app_user"
echo "[AppServerSSHPort]: $bench_app_port"
. ./csharpcli.sh

conn_str_len=$(array_len "$connection_string_list" "|")
app_server_len=$(array_len "$bench_app_pub_server" "|")
if [ "$conn_str_len" == 1 ]
then
  service_name=$(extract_servicename_from_connectionstring $connection_string_list)
fi
app_launch_log_dir=""
k8s_result_dir=""
if [ -d $result_root/$bench_type_list ]
then
  app_launch_log_dir=$result_root/${bench_type_list}
  k8s_result_dir=$result_root/$bench_type_list
else
  app_launch_log_dir=${result_dir}
  k8s_result_dir=$result_root
fi

if [ "$conn_str_len" == 1 ]
then
  start_multiple_app_server_with_single_service "$connection_string_list" "$bench_app_pub_server" $bench_app_user $bench_app_pub_port "$app_launch_log_dir"
else
  start_multiple_app_server "$connection_string_list" "$bench_app_pub_server" $bench_app_user $bench_app_pub_port "$app_launch_log_dir"
fi

start_collect_top_on_app_server "$bench_app_pub_server" $bench_app_user $bench_app_pub_port "$app_launch_log_dir"

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
   #nohup sh collect_connections.sh $service_name $k8s_result_dir &
   #collect_conn_pid=$!
   echo "nohup sh collect_pod_top.sh $service_name $k8s_result_dir &"
   nohup sh collect_pod_top.sh $service_name $k8s_result_dir &
   collect_pod_top_pid=$!
   if [ "$g_nginx_ns" != "" ]
   then
      nohup sh collect_nginx_top.sh $service_name $g_nginx_ns $k8s_result_dir &
      collect_nginx_top_pid=$!
   fi
else
   echo "It seems you are running a self-host SignalR service because the service name is not standard"
fi

sh run_csharp_cli.sh

echo "Stop app server"
stop_multiple_app_server "$bench_app_pub_server" $bench_app_user $bench_app_pub_port
stop_collect_top_on_app_server "$app_launch_log_dir"

if [ "$service_name" != "" ]
then
   #kill $collect_conn_pid
   kill $collect_pod_top_pid
   if [ "$collect_nginx_top_pid" != "" ]
   then
     kill $collect_nginx_top_pid
   fi
   if [ "$copy_syslog" == "true" ]
   then
     copy_syslog $service_name $k8s_result_dir
   fi
   if [ "$copy_nginx_log" == "true" ]
   then
     get_nginx_log $service_name "ingress-nginx" $k8s_result_dir
   fi
   get_k8s_pod_status $service_name $k8s_result_dir
fi

sh gen_html.sh $connection_string # gen_html
