#!/bin/bash
. ./multiple_vm_env.sh
. ./func_env.sh

force_sync_time_on_all_vm "$client_vm_list" $ssh_user $ssh_port
