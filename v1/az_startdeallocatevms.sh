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
        az vm start -g ${res_grp} -n "${i}" --no-wait
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
        az vm deallocate -g ${res_grp} -n "${i}" --no-wait
     fi
  done

}

function stop_all_vms_in_group()
{
  local rsg=$1
  local vm_list=""
  for i in `az vm list -g $rsg|jq ".[].name"|tr -d '"'`
  do
    if [ "$vm_list" == "" ]
    then
       vm_list="$i"
    else
       vm_list="$vm_list $i"
    fi
  done
  deallocate_vms $rsg "$vm_list"
}

function start_all_vms_in_group()
{
  local rsg=$1
  for i in `az vm list -g $rsg|jq ".[].name"|tr -d '"'`
  do
    if [ "$vm_list" == "" ]
    then
       vm_list="$i"
    else
       vm_list="$vm_list $i"
    fi
  done
  start_vms $rsg "$vm_list"
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
