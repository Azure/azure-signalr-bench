#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh
. ./csharpcli.sh

stop_multiple_app_server "$server_vm_list" $ssh_user $ssh_port
