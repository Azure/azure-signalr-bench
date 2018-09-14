#!/bin/bash
. ./func_env.sh
cat << EOF > jenkins_env.sh
connection_number=$ConnectionNumber
connection_concurrent=$ConnectionConcurrent
connection_string_list="$ConnectionStringList"
send_number="$SendNumber"
group_number="$GroupNumber"
sigbench_run_duration=$Duration
EOF

prepare_service=0
if [ $# -eq 1 ]
then
  prepare_service=1
fi

create_root_folder

if [ "$prepare_service" == "1" ]
then
  sh start_service_on_multiple_vms.sh
  sh start_collect_top_on_multiple_services.sh $result_root
fi

if [ "$sync_time" == "1" ]
then
  sh sync_time_on_allclients.sh
fi

mkdir $result_root/$bench_type_list

sh jenkins-run-csharpcli-on-multiple-serv.sh

if [ "$prepare_service" == "1" ]
then
  sh stop_collect_top_on_multiple_services.sh $result_root
  sh stop_service_on_multiple_vms.sh
fi

gen_final_report
