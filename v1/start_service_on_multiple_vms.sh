#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh

launch_service_on_all_vms "$service_vm_list" $ssh_user $ssh_port
