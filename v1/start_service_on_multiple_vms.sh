#!/bin/bash
. ./multiple_vm_env.sh
. ./build_launch_signalr_service.sh

#deploy_package_4_multiple_service_vm $raw_asrs_bin "$redis" "${service_vm_list}" $ssh_user $ssh_port
launch_service_on_all_vms "$service_vm_list" $ssh_user $ssh_port
