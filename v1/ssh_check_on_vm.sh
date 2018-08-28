#!/bin/bash
. ./multiple_vm_env.sh
. ./func_env.sh

check_all_vm_ssh "$client_vm_list" $ssh_user $ssh_port
