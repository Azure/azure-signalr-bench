#!/bin/bash
. ./func_env.sh
cat << EOF > jenkins_env.sh
connection_number=$ConnectionNumber
connection_concurrent=$ConnectionConcurrent
connection_string="$ConnectionString"
send_number="$SendNumber"
sigbench_run_duration=$Duration
EOF

create_root_folder

sh jenkins-run-csharpcli.sh

gen_final_report
