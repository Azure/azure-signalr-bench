#!/bin/bash
. ./func_env.sh

. ./build_launch_signalr_service.sh

function single_run() {
  sh jenkins-run-websocket.sh
}

function multiple_try_run() {
  local service_host=$1
  local connection_number=$2
  local step_number=$3
  local concurrent_number=$4
  local duration=$5
  local max_try=$6
  local appsetting_tmpl=$7
  local src_root_dir=$8
  local use_https=0
  local i=0

  local connection_string
  if [ $use_https == 0 ]
  then
    connection_string="Endpoint=http://$service_host;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
  else
    connection_string="Endpoint=https://$service_host;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
  fi

  local servicebin_dir=signalrservicebin
  if [ ! -e $servicebin_dir ]
  then
    mkdir $servicebin_dir
  fi

  while [ $i -lt $max_try ]
  do
cat << EOF > jenkins_env.sh
sigbench_run_duration=$duration
connection_concurrent=$concurrent_number
connection_string="$connection_string"
connection_number=$connection_number
send_number=$connection_number
bench_type_list="c${connection_number}"
use_https=$use_https
EOF
    mkdir $result_root/c$connection_number

    local service_launch_log=$result_root/c$connection_number/service_launch.log
    local cur_dir=`pwd`
    local commit_hash_file="$cur_dir"/$result_root/c$connection_number/commit_hash.txt
    build_and_launch $servicebin_dir $service_host $bench_service_user $bench_service_pub_port $service_launch_log $appsetting_tmpl $src_root_dir "$commit_hash_file"

    local build_status=$(check_build_status $servicebin_dir)
    local launch_status=$(check_service_launch_status $service_launch_log)
    if [ $build_status -ne 0 ] || [ $launch_status -ne 0 ]
    then
      echo "Build or launch failed!"
    else 
      single_run
    fi
    if [ -e $result_root/$error_mark_file ]
    then
       echo "!!!Stop trying since error occurs"
       return
    fi
    connection_number=`expr $connection_number + $step_number`
    i=`expr $i + 1`
  done
}

create_root_folder

echo "-------your inputs------"
echo "[ServiceHost]: $ServiceHost"
echo "[BaseConnectionNumber]: $BaseConnectionNumber"
echo "[ConnectionSteps]: $ConnectionSteps"
echo "[ConcurrentConnectionNumber]: $ConcurrentConnectionNumber"
echo "[Duration]: $Duration"
echo "[MaxTry]: $MaxTry"
echo "[RedisConnectString]: $RedisConnectString"

appsetting_file="$result_root/appsetting_tmpl.json"
if [ "$RedisConnectString" != "" ]
then
  sed "s/RedisConnectString/$RedisConnectString/g" servicetmpl/appsettings_redis.json > $appsetting_file
else
  appsetting_file="servicetmpl/appsettings.json"
fi

multiple_try_run $ServiceHost $BaseConnectionNumber $ConnectionSteps $ConcurrentConnectionNumber $Duration $MaxTry $appsetting_file $SignalRServiceSrcRoot

sh gen_all_units.sh # gen_all_report
sh publish_report.sh 
sh gen_summary.sh # refresh summary.html in NginxRoot gen_summary
sh send_mail.sh $HOME/NginxRoot/$result_root/allunits.html
