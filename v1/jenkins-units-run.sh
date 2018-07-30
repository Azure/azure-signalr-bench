#!/bin/bash
. ./az_signalr_service.sh

function run_unit() {
  local unit=$1
  local loc=$2
  local ClientConnectionNumber=`expr $unit \* 1000`
  az_login_signalr_dev_sub
  az group create --name $rsg --location $loc
  signalr_service=$(create_signalr_service $rsg $name $sku $unit)
  if [ "$signalr_service" == "" ]
  then
    echo "Fail to create SignalR Service"
    return
  else
    echo "Create SignalR Service ${signalr_service}"
  fi
  ConnectionString=$(query_connection_string $name $rsg)
  echo "Connection string: '$ConnectionString'"  
  # override jenkins_env.sh
cat << EOF > jenkins_env.sh
sigbench_run_duration=$Duration
connection_concurrent=200
connection_string="$ConnectionString"
connection_number=$ClientConnectionNumber
send_number=$ClientConnectionNumber
use_https=1
EOF
  sh jenkins-run.sh
  delete_signalr_service $name $rsg
}

echo "------jenkins inputs------"
echo "[Units] $UnitList"
echo "[Duration]: $sigbench_run_duration"
echo "[Location]: $Location"

if [ "$UnitList" == "all" ]
then
  for i in 1 2 3 4 5 6 7 8 9 10
  do
    run_unit $i $Location
  done
else
  run_unit $UnitList
fi
