#!/bin/bash
. ./func_env.sh
. ./multiple_vm_env.sh

timestamp=`date +%Y%m%d%H%M%S`
top_foler=/tmp/services_tops_${timestamp}
if [ ! -d $top_foler ]
then
  mkdir $top_foler
fi

collect_top_on_all_vms "$service_vm_list" $ssh_user $ssh_port $top_foler
