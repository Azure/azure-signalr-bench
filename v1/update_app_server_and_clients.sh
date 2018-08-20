#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh

update_azure_signalr_bench_appserver "$server_vm_list" $ssh_user $ssh_port

update_azure_signalr_bench_client "$client_vm_list" $ssh_user $ssh_port
