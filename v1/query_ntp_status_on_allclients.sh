#!/bin/bash
. ./multiple_vm_env.sh
. ./func_env.sh

get_ntpq_stat_on_all_vm "$client_vm_list" $ssh_user $ssh_port
