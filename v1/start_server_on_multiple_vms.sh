#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh
. ./csharpcli.sh

app_log_folder="/tmp/appserver_`date +%Y%m%d%H%M%S`"

if [ ! -e $app_log_folder ]
then
   mkdir $app_log_folder
fi

service_len=$(array_get "$service_vm_list" "|")
server_len=$(array_get "$server_vm_list" "|")
if [ "$service_len" != "$server_len" ]
then
  echo "service vm number ($service_len) != server vm number ($server_len)"
  exit 1
fi

conn_str_list=$(gen_connection_string_list_from_multiple_service "$service_vm_list")
start_multiple_app_server "$conn_str_list" "$server_vm_list" $ssh_user $ssh_port $app_log_folder
