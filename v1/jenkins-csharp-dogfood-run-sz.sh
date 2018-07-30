#!/bin/bash
. ./az_signalr_service.sh

. ./func_env.sh
. ./kubectl_utils.sh
## Required parameters: 
#  Location, UnitList, Duration, SendSizeList
#  EchoConnectionNumberList, EchoSendNumberList, EchoConcurrentConnectNumberList
#  BroadcastConnectionNumberList, BroadcastSendNumberList, BroadcastConcurrentConnectNumberList

## Optional parameters:
#  DisableThrottling, EchoStepList, BroadcastStepList, MaxRetry

echo "------jenkins inputs------"
echo "[Units] '$UnitList'"
echo "[Duration]: '$Duration'"
echo "[Location]: '$Location'"
echo "[SendMsgSizeList]: '$SendSizeList'"
echo "[EchoConnectionNumberList]: '$EchoConnectionNumberList'"
echo "[EchoSendNumberList]: '$EchoSendNumberList'"
echo "[EchoConcurrentConnectNumberList]: '$EchoConcurrentConnectNumberList'"
echo "[BroadcastConnectionNumberList]: '$BroadcastConnectionNumberList'"
echo "[BroadcastConcurrentConnectNumberList]: '$BroadcastConcurrentConnectNumberList'"
echo "-----optional inputs-----"
echo "[DisableThrottling]: '$DisableThrottling'"
echo "[EchoStepList]: '$EchoStepList'"
echo "[BroadcastStepList]: '$BroadcastStepList'"
echo "[MaxRetry]: '$MaxRetry'"

target_grp="honzhanatperf"`date +%M%S`
sku="Basic_DS2"
location=$Location

function run_unit_benchmark() {
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4
  local sendsize=$5

  local signalr_service
  local normal_unit=$unit
  if [ $normal_unit -gt 10 ]
  then
    normal_unit=10
  fi
  signalr_service=$(create_signalr_service $rsg $name $sku $normal_unit)
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

  if [ "$DisableThrottling" == "true" ]
  then
    patch_connection_throttling_env $name 5000000
  fi

  local ConnectionString=$(query_connection_string $name $rsg)
  echo "Connection string: '$ConnectionString'"

  local bench_type_tag
  local echo_connection_number=$(array_get $EchoConnectionNumberList $unit "|")
  local echo_send_number=$(array_get $EchoSendNumberList $unit "|")
  local echo_concurrent_number=$(array_get $EchoConcurrentConnectNumberList $unit "|")
  local broadcast_connection_number=$(array_get $BroadcastConnectionNumberList $unit "|")
  local broadcast_send_number=$(array_get $BroadcastSendNumberList $unit "|")
  local broadcast_concurrent_number=$(array_get $BroadcastConcurrentConnectNumberList $unit "|")
  # override jenkins_env.sh
cat << EOF > jenkins_env.sh
connection_number=$echo_connection_number
connection_concurrent=$echo_concurrent_number
send_number=$echo_send_number
sigbench_run_duration=$Duration
echoconnection_number=$echo_connection_number
echoconnection_concurrent=$echo_concurrent_number
echosend_number=$echo_send_number
broadcastconnection_number=$broadcast_connection_number
broadcastconnection_concurrent=$broadcast_concurrent_number
broadcastsend_number=$broadcast_send_number
connection_string="$ConnectionString"
bench_type_list=$bench_type_tag
bench_send_size=$sendsize
use_https=1
EOF
  if [ "$EchoStepList" != "" ] && [ "$BroadcastStepList" != "" ] && [ $MaxRetry -gt 0 ]
  then
    local i=0
    local echostep=$(array_get $EchoStepList $unit "|")
    local broadcaststep=$(array_get $BroadcastStepList $unit "|")
    while [ $i -lt $MaxRetry ]
    do
       local name_list=""
       local existing=`echo "$bench_name_list"|grep "echo"`
       if [ "$existing" != "" ]
       then
          name_list=${name_list}"e${echo_connection_number}"
       fi
       existing=`echo "$bench_name_list"|grep "broadcast"`
       if [ "$existing" != "" ]
       then
          name_list=${name_list}"b${broadcast_connection_number}"
       fi
       if [ $sendsize != "0" ]
       then
          name_list=${name_list}"_${sendsize}"
       fi
       bench_type_tag="unit${unit}_${name_list}"

cat << EOF > jenkins_env.sh
connection_number=$echo_connection_number
connection_concurrent=$echo_concurrent_number
send_number=$echo_send_number
sigbench_run_duration=$Duration
echoconnection_number=$echo_connection_number
echoconnection_concurrent=$echo_concurrent_number
echosend_number=$echo_send_number
broadcastconnection_number=$broadcast_connection_number
broadcastconnection_concurrent=$broadcast_concurrent_number
broadcastsend_number=$broadcast_send_number
connection_string="$ConnectionString"
bench_type_list=$bench_type_tag
bench_send_size=$sendsize
use_https=1
EOF
       mkdir $result_root/$bench_type_tag
       sh jenkins-run-csharpcli.sh
       if [ -e $result_root/$error_mark_file ]
       then
         echo "!!!Stop trying ($i) since error occurs"
         break
       fi
       echo_connection_number=$((echo_connection_number + $echostep))
       echo_send_number=$((echo_send_number + $echostep))
       broadcast_connection_number=$((broadcast_connection_number + $broadcaststep))
       broadcast_send_number=$((broadcast_send_number + $broadcaststep))
       broadcast_concurrent_number=$broadcast_connection_number
       i=`expr $i + 1`
    done
  else
    # create unit folder before run-websocket because it may require that folder
    mkdir $result_root/$bench_type_tag
    sh jenkins-run-csharpcli.sh
  fi
  delete_signalr_service $name $rsg
}

function run_units() {
  local grp=$1
  local sku=$2
  local unitlist=$3
  local sizelist=$4
  local list
  local i j
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
     if [ $i -eq 11 ]
     then
       DisableThrottling="true"
     fi
     for j in $sizelist
     do
        name="autoperf"`date +%H%M%S`
        run_unit_benchmark $grp $name $sku $i $j
     done
  done
  gen_final_report 
}

register_signalr_service_dogfood

az_login_ASRS_dogfood

create_group_if_not_exist $target_grp $location

run_units $target_grp $sku "$UnitList" "$SendSizeList"

delete_group $target_grp

unregister_signalr_service_dogfood
