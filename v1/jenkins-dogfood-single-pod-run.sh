#!/bin/bash
. ./az_signalr_service.sh

. ./func_env.sh

. ./kubectl_utils.sh

function run_signalr_service() {
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4

  local signalr_service=$(create_signalr_service $rsg $name $sku $unit)
  if [ "$signalr_service" == "" ]
  then
    echo "Fail to create SignalR Service"
    return
  else
    echo "Create SignalR Service ${signalr_service}"
  fi
  local dns_ready=$(check_signalr_service_dns $rsg $name)
  if [ $dns_ready -eq 1 ]
  then
    echo "SignalR Service DNS is not ready, suppose it is failed!"
    delete_signalr_service $name $rsg
    return
  fi
  local ConnectionString=$(query_connection_string $name $rsg)
  echo "Connection string: '$ConnectionString'"

  local replica=1
  if [ "$g_require_patch" == "1" ]
  then
    patch_and_wait $name $rsg $unit $replica
    local status=$(check_signalr_service_dns $rsg $name)
    if [ $status == 1 ]
    then
      echo "!!!Provisioning SignalR service failed!!!"
      delete_signalr_service $name $rsg
      return
    fi
  fi
  local base_connection_number=$(array_get $g_base_connection_number_list $unit "|")
  local connection_step=$(array_get $g_connection_step_list $unit "|")
  local duration=$(array_get $g_duration_list $unit "|")
  local max_try=$(array_get $g_max_try_list $unit "|")
  local concurrent_connection=$(array_get $g_concurrent_connection_number_list $unit "|")

  multiple_try_run $ConnectionString $base_connection_number $connection_step $concurrent_connection $duration $max_try $unit

  delete_signalr_service $name $rsg
}

function single_run() {
  sh jenkins-run-websocket.sh
}

function multiple_try_run() {
  local connection_string=$1
  local connection_number=$2
  local step_number=$3
  local concurrent_number=$4
  local duration=$5
  local max_try=$6
  local core=$7
  local i=0
  local use_https
  if [[ $connection_string = *"https://"* ]]
  then
     use_https=1
  else
     use_https=0
  fi

  while [ $i -lt $max_try ]
  do
cat << EOF > jenkins_env.sh
sigbench_run_duration=$duration
connection_concurrent=$concurrent_number
connection_string="$connection_string"
connection_number=$connection_number
send_number=$connection_number
bench_type_list="cpu${core}_c${connection_number}"
use_https=$use_https
EOF
    mkdir $result_root/cpu${core}_c$connection_number
    single_run
    if [ -e $result_root/$error_mark_file ]
    then
       echo "!!!Stop trying since error occurs"
       return
    fi
    connection_number=`expr $connection_number + $step_number`
    i=`expr $i + 1`
  done
}

function run_all_pods() {
  local grp=$1
  local sku=$2
  local name
  create_root_folder
  for m in 1 2 3 4
  do
    name="autopod"`date +%H%M%S`
    run_signalr_service $grp $name $sku $m
  done
  gen_final_report
}

target_grp="honzhanautopod"`date +%M%S`
sku="Basic_DS2"
location=$Location
g_base_connection_number_list=$BaseConnectionNumberList
g_connection_step_list=$ConnectionStepList
g_concurrent_connection_number_list=$ConcurrentConnectionNumberList
g_max_try_list=$MaxTryList
g_duration_list=$DurationList

echo "-------your inputs------"
echo "[Location]: $location"
echo "[BaseConnectionNumberList]: $g_base_connection_number_list"
echo "[ConnectionStepList]: $g_connection_step_list"
echo "[ConcurrentConnectionNumberList]: $g_concurrent_connection_number_list"
echo "[DurationList]: $g_duration_list"
echo "[MaxTryList]: $g_max_try_list"

register_signalr_service_dogfood

az_login_ASRS_dogfood

create_group_if_not_exist $target_grp $location

run_all_pods $target_grp $sku

delete_group $target_grp

unregister_signalr_service_dogfood

