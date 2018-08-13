#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh
. ./csharpcli.sh
conn_str_list=$(gen_connection_string_list_from_multiple_service "$service_vm_list")
echo "$conn_str_list"
