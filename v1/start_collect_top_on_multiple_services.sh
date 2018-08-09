#!/bin/bash
. ./func_env.sh
. ./multiple_vm_env.sh

top_folder=""
if [ $# -eq 1 ]
then
  top_folder="$1"
else
  timestamp=`date +%Y%m%d%H%M%S`
  top_folder=/tmp/services_tops_${timestamp}
fi

if [ ! -d $top_folder ]
then
  mkdir $top_folder
fi

collect_top_on_all_vms "$service_vm_list" $ssh_user $ssh_port $top_folder
