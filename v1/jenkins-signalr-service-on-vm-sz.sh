#!/bin/bash
## Required parameters:
# ResourceGroup, VMSizeList, VMHostPrefixList,
# SendSizeList, SendIntervalList
# EchoConnectionNumberList_XXX_XXX, EchoSendNumberList_XXX_XXX, EchoConcurrentConnectNumberList_XXX_XXX, EchoStepList_XXX_XXX
# BroadcastConnectionNumberList_XXX_XXX, BroadcastSendNumberList_XXX_XXX, BroadcastConcurrentConnectNumberList_XXX_XXX, BroadcastStepList_XXX_XXX
# RedisConnectString, Duration, MaxTryList, GitBranch
. ./utils.sh
. ./func_env.sh
. ./build_launch_signalr_service.sh

echo "-------Your Jenkins Inputs------"
echo "[SendSizeList]: $SendSizeList"
echo "[SendIntervalList]: $SendIntervalList"
echo "[Duration]: $Duration"
echo "[MaxTryList]: $MaxTryList"
echo "[VMSizeList]: $VMSizeList"
echo "[VMHostPrefixList]: $VMHostPrefixList"
echo "[GitBranch]: $GitBranch"
echo "[RedisConnectString]: $RedisConnectString"
echo "[ResourceGroup]: $ResourceGroup"

function multiple_try_run() {
  local servicebin_dir=$g_output_dir
  local service_host=$g_service_host
  local duration=$g_duration
  local max_try=$g_max_try
  local echo_connection_number=$g_echo_connection_number
  local echo_concurrent_connection_number=$g_echo_concurrent_connection_number
  local echo_send_number=$g_echo_send_number
  local echo_step=$g_echo_connection_step
  local broadcast_step=$g_broadcast_connection_step
  local broadcast_connection_number=$g_broadcast_connection_number
  local broadcast_concurrent_connection_number=$g_broadcast_concurrent_connection_number
  local broadcast_send_number=$g_broadcast_send_number

  local i=0

  local connection_string
  connection_string="Endpoint=http://$service_host;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"

  local tag
  while [ $i -lt $max_try ]
  do
    local normal_vmsize=`echo "${g_vmsize}"|sed -e 's/_//g'` # remove '_'
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

    tag="${normal_vmsize}_${name_list}"
cat << EOF > jenkins_env.sh
bench_send_size=$g_send_size
sigbench_run_duration=$duration
echosend_interval=$g_send_interval
echoconnection_number=$echo_connection_number
echoconnection_concurrent=$echo_concurrent_connection_number
echosend_number=$echo_send_number
broadcastsend_interval=$g_send_interval
broadcastconnection_number=$broadcast_connection_number
broadcastconnection_concurrent=$broadcast_concurrent_connection_number
broadcastsend_number=$broadcast_send_number
connection_concurrent=$echo_concurrent_connection_number
connection_number=$echo_connection_number
send_number=$echo_send_number
bench_type_list="${tag}"
use_https=0
connection_string="$connection_string"
EOF
    mkdir $result_root/$tag

    local service_launch_log=$result_root/$tag/service_launch.log
    launch_service $servicebin_dir $service_host $bench_service_user $bench_service_pub_port $service_launch_log

    local launch_status=$(check_service_launch_status $service_launch_log)
    if [ $launch_status -ne 0 ]
    then
      echo "Launch failed!"
      return
    fi

    sh jenkins-run-websocket.sh
    stop_service $service_host $bench_service_user $bench_service_pub_port
    cd $result_root/$tag/
    tar zcvf service_launch.log.tgz service_launch.log
    rm service_launch.log
    cd -
    if [ -e $result_root/$error_mark_file ]
    then
       echo "!!Continue to run even though there is error occurs!!"
       #echo "!!!Stop trying since error occurs"
       #return
    fi
    echo_connection_number=$((echo_connection_number + $echo_step))
    echo_send_number=$((echo_send_number + $echo_step))
    broadcast_connection_number=$((broadcast_connection_number + $broadcast_step))
    broadcast_send_number=$((broadcast_send_number + $broadcast_step))
    broadcast_concurrent_number=$broadcast_connection_number
    i=`expr $i + 1`
  done
}

git_clone_and_build() {
  local servicebin_dir=$1
  local git_branch=$2
  if [ -e $servicebin_dir ]
  then
    rm -fr $servicebin_dir
  fi
  mkdir $servicebin_dir

  local src_dir=/tmp
  local src_prefix="ASRS-"
  # clear the dir older than 5 days
  find $src_dir -mtime +5 -name "${src_prefix}*" 2>/dev/null|grep "${src_prefix}"|xargs rm -rf

  local src_root_dir=$src_dir/${src_prefix}`date +%Y%m%d%H%M%S`
  local service_launch_log=$result_root/service_launch.log
  local cur_dir=`pwd`
  local commit_hash_file="$cur_dir"/$result_root/commit_hash.txt

  git_clone $src_root_dir $git_branch

  build_signalr_service $src_root_dir "$cur_dir"/$servicebin_dir "$commit_hash_file"

  local status=$(check_build_status $servicebin_dir)
  if [ $status != 0 ]
  then
    echo 1
  else
    echo 0
  fi
}

create_target_single_service_vm() {
  local rsg=$1
  local name_prefix=$2
  local vm_size=$3

  cd AzureAccess
  dotnet run -- -a ../signalr_dev.auth \
              -i ${bench_image_resource_group} \
              -n ${bench_image_name} -s $rsg \
              -p ${name_prefix} -S ${vm_size} \
              -H $HOME/.ssh/id_rsa.pub -c 1 \
              -u ${bench_vm_user} -O vmhost.txt \
              -A True -m accelerated_network_vmsize.txt \
              -z ${bench_vm_ssh_port}
  local hostname=`cat vmhost.txt`
  g_ServiceHost=$hostname
  cd -
  echo "========check accelerated networking========="
  ssh -o StrictHostKeyChecking=no -p ${bench_vm_ssh_port} ${bench_vm_user}@${hostname} "lspci"
  ssh -o StrictHostKeyChecking=no -p ${bench_vm_ssh_port} ${bench_vm_user}@${hostname} "ethtool -S eth0 | grep vf_"
}

iterate_on_configuration() {
 local callback=$1
 local vm_size=$2
 local ServiceHost=$3
 local output_dir=$4
 local unit=$5
 local SendSizeLen=$(array_len "$SendSizeList" "|")
 local SendIntervalLen=$(array_len "$SendIntervalList" "|")
 local echo_connection_number_prefix="EchoConnectionNumberList"
 local echo_send_number_prefix="EchoSendNumberList"
 local echo_concurrent_number_prefix="EchoConcurrentConnectNumberList"
 local echo_step_prefix="EchoStepList"
 
 local broadcast_connection_number_prefix="BroadcastConnectionNumberList"
 local broadcast_send_number_prefix="BroadcastSendNumberList"
 local broadcast_concurrent_number_prefix="BroadcastConcurrentConnectNumberList"
 local broadcast_step_prefix="BroadcastStepList"

 local i=1 j k
 local item
 while [ $i -le $SendSizeLen ]
 do
   g_send_size=$(array_get $SendSizeList $i "|")
   #echo $g_send_size
   j=1
   while [ $j -le $SendIntervalLen ]
   do
     g_send_interval=$(array_get $SendIntervalList $j "|")
     #echo $g_send_interval
     EchoConnectionNumberList=$(derefer_3vars "$echo_connection_number_prefix" "_"$g_send_size "_"$g_send_interval)
     EchoSendNumberList=$(derefer_3vars "$echo_send_number_prefix" "_"$g_send_size "_"$g_send_interval)
     EchoConcurrentConnectNumberList=$(derefer_3vars "$echo_concurrent_number_prefix" "_"$g_send_size "_"$g_send_interval)
     EchoStepList=$(derefer_3vars "$echo_step_prefix" "_"$g_send_size "_"$g_send_interval)

     BroadcastConnectionNumberList=$(derefer_3vars "$broadcast_connection_number_prefix" "_"$g_send_size "_"$g_send_interval)
     BroadcastSendNumberList=$(derefer_3vars "$broadcast_send_number_prefix" "_"$g_send_size "_"$g_send_interval)
     BroadcastConcurrentConnectNumberList=$(derefer_3vars "$broadcast_concurrent_number_prefix" "_"$g_send_size "_"$g_send_interval)
     BroadcastStepList=$(derefer_3vars "$broadcast_step_prefix" "_"$g_send_size "_"$g_send_interval)
     #echo "$echo_connection_number_list"

     k=$unit
     local echo_connection_number=$(array_get $EchoConnectionNumberList $k "|")
     local echo_send_number=$(array_get $EchoSendNumberList $k "|")
     local echo_concurrent_number=$(array_get $EchoConcurrentConnectNumberList $k "|")
     local echo_step=$(array_get $EchoStepList $k "|")
     local broadcast_connection_number=$(array_get $BroadcastConnectionNumberList $k "|")
     local broadcast_send_number=$(array_get $BroadcastSendNumberList $k "|")
     local broadcast_concurrent_number=$(array_get $BroadcastConcurrentConnectNumberList $k "|")
     local broadcast_step=$(array_get $BroadcastStepList $k "|")

     g_max_try=$(array_get $MaxTryList $k "|")
     g_service_host=$ServiceHost
     g_echo_connection_number=$echo_connection_number
     g_echo_send_number=$echo_send_number
     g_echo_concurrent_connection_number=$echo_concurrent_number
     g_echo_connection_step=$echo_step
     g_echo_send_interval=$g_send_interval
     g_broadcast_connection_number=$broadcast_connection_number
     g_broadcast_send_number=$broadcast_send_number
     g_broadcast_concurrent_connection_number=$broadcast_concurrent_number
     g_broadcast_connection_step=$broadcast_step
     g_broadcast_send_interval=$g_send_interval
     g_duration=$Duration
     g_vmsize=$vm_size
     g_output_dir=$output_dir
     $callback
     j=$((j+1))
   done
   i=$((i+1))
 done
}

run_all() {
 local output_dir=ASRSBin
 git_clone_and_build $output_dir $GitBranch
 local status=$(check_build_status $output_dir)
 if [ "$status" != "0" ]
 then
   echo "Fail to build SignalR Service!!!"
   return
 fi

 local vm_size vm_host_prefix
 local len=$(array_len "$VMSizeList" "|")
 local i=1
 while [ $i -le $len ]
 do
   vm_size=$(array_get $VMSizeList $i "|")
   vm_host_prefix=$(array_get $VMHostPrefixList $i "|")
   g_max_try=$(array_get $MaxTryList $i "|")
   ## create VM
   g_ServiceHost=""
   create_target_single_service_vm $ResourceGroup $vm_host_prefix $vm_size
   ## pass to global service server where CPU usage will be collected
cat << EOF >> servers_env.sh
bench_service_pub_server=$g_ServiceHost
EOF
   ## Configure Service
   local uuid=`cat /proc/sys/kernel/random/uuid`
   g_appsetting_file="$result_root/appsetting_tmpl.json"
   if [ "$RedisConnectString" != "" ]
   then
     sed -e "s/RedisConnectString/$RedisConnectString/g;s/dev/$uuid/g" servicetmpl/appsettings_redis.json > $g_appsetting_file
   else
     g_appsetting_file="servicetmpl/appsettings.json"
   fi
   replace_appsettings $output_dir $g_ServiceHost $g_appsetting_file
   zip_signalr_service $output_dir
   ## generate configuration
   iterate_on_configuration multiple_try_run $vm_size $g_ServiceHost $output_dir $i
   i=$(($i + 1))
 done
}

create_root_folder

run_all

gen_final_report
