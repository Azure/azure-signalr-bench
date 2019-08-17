#!/bin/bash

bench_start_file=auto_start_sigbench.sh
bench_stop_file=auto_stop_sigbench.sh

sigbench_home=sigbench
sigbench_name=websocket-bench
sigbench_master_starter=run_sigbench_master.sh
sigbench_render_script=render_tmpl.py
sigbench_env_file=autogen_bench_env.sh
sigbench_config_dir=configs
sigbench_config_file=config.yaml
sigbench_output_dir=benchout
sigbench_log_file=log.txt
sigbench_agent_output=agent.out
sigbench_pid_file=/tmp/sigbench.pid
sigbench_norm_file=sigbench_normal.txt

signalr_src_root=$HOME/OSSServices-SignalR-Service
signalr_sdk_src_root=$HOME/azure-signalr
signalr_service_package=signalrservice
signalr_core_package=signalrcore
signalr_bench_demo=signalrdemo
signalr_build_dist=signalr_dist

signalr_service_name=SignalRServiceSample
signalr_service_app_name=LatencyService
signalr_core_app_name=Latency

app_running_log=bench_app_running.log
#result_name=${bench_name}_${bench_type}_${bench_codec}
#result_root=`date +%Y%m%d%H%M%S`
result_dir=${result_root}
html_dir=$result_dir

cmd_file_prefix=auto_cmds
cmd_config_prefix="cmd_4"
websocket_script_prefix=autorun_websocket
cli_script_prefix=autorun_sigcli
error_mark_file=error.mark
