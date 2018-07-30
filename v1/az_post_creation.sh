#!/bin/bash
. ./az_vm_instances_manage.sh

echo "---------------------------"
date

az_login_signalr_dev_sub

restart_all_vms # restart VM to make 22222 take effect

sleep 120

ecdd_user_pub_key_for_all_vms pubkey/benchserver_id_rsa.pub
add_user_pub_key_for_all_vms pubkey/singlecpu_id_rsa.pub

setup_benchmark_on_all_clients
echo "---------------------------"
date
