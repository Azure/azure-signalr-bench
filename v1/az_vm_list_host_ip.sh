#!/bin/bash
. ./az_vm_instances_manage.sh

echo "---------------------------"
date

#az_login_signalr_dev_sub
#az_login_jenkins_sub
az_login_java_test_7ttl

g_location=get_vm_img_location $g_myimg_rsg_name $g_myimg_name

set +x

gen_ssh_access_endpoint_for_signalr_bench

list_all_pubip

echo "---------------------------"
date
