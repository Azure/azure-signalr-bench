#!/bin/bash

## Required parameters:
# EchoConnectionNumber, EchoConcurrentConnectionNumber,
# BroadcastConnectionNumber, BroadcastConcurrentConnectionNumber,
# Duration, SendSize, NginxServer, UseHttps, ServerHost
# BenchNameList, BenchCodecList, AppBenchPort

echo "-------jenkins normalize your inputs------"
echo "[ServerHost]: $ServerHost"
echo "[EchoConnectionNumber]: $EchoConnectionNumber"
echo "[EchoConcurrentNumber]: $EchoConcurrentConnectionNumber"
echo "[BroadcastConnectionNumber]: $BroadcastConnectionNumber"
echo "[BroadcastConcurrentNumber]: $BroadcastConcurrentConnectionNumber"
echo "[Duration]: $Duration"
echo "[UseHttps]: $UseHttps"
echo "[SendSize]: $SendSize"
echo "[NginxServer]: $NginxServer"
echo "[BenchNameList]: '$BenchNameList'"
echo "[BenchCodecList]: '$BenchCodecList'"
echo "[AppBenchPort]: $AppBenchPort"

gen_jenkins_env_from_in_parameters() {
 local use_https
 if [ "$UseHttps" == "true" ]
 then
   use_https="1"
 else
   use_https="0"
 fi

cat << EOF >> servers_env.sh
bench_app_pub_server=$ServerHost
bench_app_port=$AppBenchPort
EOF

cat << EOF > jenkins_env.sh
## replace builtin variable
bench_config_hub="chat"
bench_codec_list="$BenchCodecList"
bench_send_size=$SendSize
bench_name_list="$BenchNameList"
echoconnection_number=$EchoConnectionNumber
echoconnection_concurrent=$EchoConcurrentConnectionNumber
echosend_number=$EchoConnectionNumber
broadcastconnection_number=$BroadcastConnectionNumber
broadcastconnection_concurrent=$BroadcastConcurrentConnectionNumber
broadcastsend_number=$BroadcastConnectionNumber
nginx_server_dns=$NginxServer
use_https=$use_https
EOF
}

gen_jenkins_env_from_in_parameters

. ./func_env.sh

create_root_folder

gen_jenkins_command_config

sh run_websocket.sh
sh gen_html.sh

gen_final_report
