#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh

check_all_service_client_connection "$service_vm_list" $ssh_user $ssh_port
