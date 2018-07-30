#!/bin/bash
## Required parameters
# Location,
# EchoConnectionNumberList, EchoSendNumberList, EchoConcurrentConnectNumberList, EchoStepList
# BroadcastConnectionNumberList, BroadcastSendNumberList, BroadcastConcurrentConnectNumberList, BroadcastStepList
# DurationList, MaxTryList

. ./az_signalr_service.sh

. ./func_env.sh

. ./kubectl_utils.sh

target_grp="honzhanautopod"`date +%M%S`
sku="Basic_DS2"

echo "-------your inputs------"
echo "[Location]: $Location"
echo "[SendMsgSizeList]: '$SendSizeList'"
echo "[EchoConnectionNumberList]: '$EchoConnectionNumberList'"
echo "[EchoSendNumberList]: '$EchoSendNumberList'"
echo "[EchoConcurrentConnectNumberList]: '$EchoConcurrentConnectNumberList'"
echo "[BroadcastConnectionNumberList]: '$BroadcastConnectionNumberList'"
echo "[BroadcastSendNumberList]: '$BroadcastSendNumberList'"
echo "[BroadcastConcurrentConnectNumberList]: '$BroadcastConcurrentConnectNumberList'"
echo "[EchoStepList]: '$EchoStepList'"
echo "[BroadcastStepList]: '$BroadcastStepList'"
echo "[DurationList]: $DurationList"
echo "[MaxTryList]: $MaxTryList"

function run_signalr_service() {
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4
  local send_size=$5

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
  g_connection_string=$ConnectionString
  g_echo_connection_number=$(array_get $EchoConnectionNumberList $unit "|")
  g_echo_send_number=$(array_get $EchoSendNumberList $unit "|")
  g_echo_concurrent_number=$(array_get $EchoConcurrentConnectNumberList $unit "|")
  g_echo_step=$(array_get $EchoStepList $unit "|")
  g_broadcast_connection_number=$(array_get $BroadcastConnectionNumberList $unit "|")
  g_broadcast_send_number=$(array_get $BroadcastSendNumberList $unit "|")
  g_broadcast_concurrent_number=$(array_get $BroadcastConcurrentConnectNumberList $unit "|")
  g_broadcast_step=$(array_get $BroadcastStepList $unit "|")
  g_max_try=$(array_get $MaxTryList $unit "|")
  g_duration=$(array_get $DurationList $unit "|")
  g_unit=$unit
  g_send_size=$send_size

  multiple_try_run

  delete_signalr_service $name $rsg
}

function single_run() {
  sh jenkins-run-websocket.sh
}

function multiple_try_run() {
  local echo_connection_number=$g_echo_connection_number
  local echo_send_number=$g_echo_send_number
  local echo_concurrent_connection_number=$g_echo_concurrent_number
  local echo_step=$g_echo_step
  local broadcast_connection_number=$g_broadcast_connection_number
  local broadcast_send_number=$g_broadcast_send_number
  local broadcast_concurrent_connection_number=$g_broadcast_concurrent_number
  local broadcast_step=$g_broadcast_step
  local max_try=$g_max_try
  local duration=$g_duration
  local unit=$g_unit
  local i=0
  local use_https
  if [[ $g_connection_string = *"https://"* ]]
  then
     use_https=1
  else
     use_https=0
  fi

  while [ $i -lt $max_try ]
  do
    local tag="cpu${unit}_e${echo_connection_number}b${broadcast_connection_number}"
cat << EOF > jenkins_env.sh
connection_string="$g_connection_string"
bench_send_size=$g_send_size
sigbench_run_duration=$duration
echoconnection_number=$echo_connection_number
echoconnection_concurrent=$echo_concurrent_connection_number
echosend_number=$echo_send_number
broadcastconnection_number=$broadcast_connection_number
broadcastconnection_concurrent=$broadcast_concurrent_connection_number
broadcastsend_number=$broadcast_send_number
connection_concurrent=$echo_concurrent_connection_number
connection_number=$echo_connection_number
send_number=$echo_send_number
bench_type_list="${tag}"
use_https=$use_https
EOF
    mkdir $result_root/$tag
    single_run
    if [ -e $result_root/$error_mark_file ]
    then
       echo "!!!Stop trying since error occurs"
       return
    fi
    echo_connection_number=$((echo_connection_number + $echo_step))
    echo_send_number=$((echo_send_number + $echo_step))
    broadcast_connection_number=$((broadcast_connection_number + $broadcast_step))
    broadcast_send_number=$((broadcast_send_number + $broadcast_step))
    broadcast_concurrent_number=$broadcast_connection_number
    i=$(($i + 1))
  done
}

function run_all_pods() {
  local grp=$1
  local sku=$2
  local name
  local m n
  create_root_folder
  for m in 1 2 3 4
  do
    for n in $SendSizeList
    do
      name="autopod"`date +%H%M%S`
      run_signalr_service $grp $name $sku $m $n
    done
  done
  gen_final_report
}

register_signalr_service_dogfood

az_login_ASRS_dogfood

create_group_if_not_exist $target_grp $Location

run_all_pods $target_grp $sku

delete_group $target_grp

unregister_signalr_service_dogfood

