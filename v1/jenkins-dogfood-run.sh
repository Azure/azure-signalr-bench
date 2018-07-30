#!/bin/bash
. ./az_signalr_service.sh

. ./func_env.sh
. ./kubectl_utils.sh

target_grp="honzhanatperf"`date +%M%S`
sku="Basic_DS2"
location=$Location

function run_unit_benchmark() {
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4
  local ClientConnectionNumber=`expr $unit \* 1000`

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
  # override jenkins_env.sh
cat << EOF > jenkins_env.sh
sigbench_run_duration=$Duration
connection_concurrent=$ConcurrentConnectionNumber
connection_string="$ConnectionString"
connection_number=$ClientConnectionNumber
send_number=$ClientConnectionNumber
bench_type_list="unit${unit}"
use_https=1
EOF

  # create unit folder before run-websocket because it may require that folder
  mkdir $result_root/unit${unit}

  sh jenkins-run-websocket.sh

  delete_signalr_service $name $rsg
}

function run_units() {
  local grp=$1
  local sku=$2
  local unitlist=$3
  local list
  local i
  local name
  create_root_folder

  if [ "$unitlist" == "all" ]
  then
    list="1 2 3 4 5 6 7 8 9 10"
  else
    list=$unitlist
  fi

  for i in $list
  do
     name="autoperf"`date +%H%M%S`
     run_unit_benchmark $grp $name $sku $i
  done
  gen_final_report 
}

echo "------jenkins inputs------"
echo "[Units] $UnitList"
echo "[Duration]: $sigbench_run_duration"
echo "[Location]: $Location"

register_signalr_service_dogfood

az_login_ASRS_dogfood

create_group_if_not_exist $target_grp $location

run_units $target_grp $sku "$UnitList"

delete_group $target_grp

unregister_signalr_service_dogfood
