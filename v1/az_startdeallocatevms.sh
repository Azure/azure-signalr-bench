#!/bin/bash
. ./utils.sh

#resource_group="honzhansignalr"
#vm_list="honzhanubuntu1 honzhanubuntu161 honzhanubuntu162"

function start_vms() {
  local res_grp=$1
  local vm_list="$2"
  local stats
  local starting="0"
  for i in $vm_list
  do
     stats=`az vm get-instance-view --name ${i} -g ${res_grp} --query instanceView.statuses[1].displayStatus`
     echo "${i}:$stats"
     if [ "$stats" == "\"VM deallocated\"" ] || [ "$stats" == "\"VM stopped\"" ]
     then
        echo "VM is stopped, and it will be started"
        az vm start -g ${res_grp} -n "${i}"
        starting="1"
     fi
  done
  if [ "$starting" == "1" ]
  then
     sleep 120
  fi
}

function deallocate_vms() {
  local res_grp=$1
  local vm_list="$2"
  local stats
  for i in $vm_list
  do
     stats=`az vm get-instance-view --name ${i} -g ${res_grp} --query instanceView.statuses[1].displayStatus`
     echo "${i}:$stats"
     if [ "$stats" == "\"VM running\"" ]
     then
        echo "VM is running, and it will be stopped"
        az vm deallocate -g ${res_grp} -n "${i}"
     fi
  done

}

#vmss_name="honzhanvmss"
#vmss_ids="3 4 5"
function start_vmss() {
  local res_grp=$1
  local vmss=$2
  local stats
  for i in $vmss_ids
  do
     stats=`az vmss get-instance-view --instance-id $i -g $res_grp -n $vmss --query statuses[1].displayStatus`
     echo "${i}:$stats"
     if [ "$stats" == "\"VM deallocated\"" ] || [ "$stats" == "\"VM stopped\"" ]
     then
        echo "VM is stopped, and it will be started"
        az vmss start -g ${res_grp} -n $vmss --instance-ids "${i}"
     fi
  done
}

function deallocate_vmss() {
  local res_grp=$1
  local vmss=$2
  local stats
  for i in $vmss_ids
  do
     stats=`az vmss get-instance-view --instance-id $i -g $res_grp -n $vmss --query statuses[1].displayStatus`
     echo "${i}:$stats"
     if [ "$stats" == "\"VM running\"" ]
     then
        echo "VM is running, and it will be stopped"
        az vmss deallocate -g ${res_grp} -n $vmss --instance-ids "${i}"
     fi
  done
}
#az_login
#start_vms $resource_group
