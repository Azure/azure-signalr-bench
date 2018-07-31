#!/bin/bash
. ./func_env.sh

bench_slave_folder=/home/${bench_app_user}/azure-signalr-bench/v2/signalr_bench/Rpc/Bench.Server
bench_master_folder=/home/${bench_app_user}/azure-signalr-bench/v2/signalr_bench/Rpc/Bench.Client
bench_server_folder=/home/${bench_app_user}/azure-signalr-bench/v2/signalr_bench/AppServer
cli_bench_start_script=autorun_start_cli_agent.sh
cli_bench_stop_script=autorun_stop_cli_agent.sh
cli_bench_agent_output=cli_agent_out.txt

gen_cli_agent_bench()
{
	local start_file=$1
	local stop_file=$2
	local output_file=$3
cat << EOF > $start_file
#!/bin/bash
pid=\`cat /tmp/agent.pid\`
kill -9 \$pid

/home/${bench_app_user}/.dotnet/dotnet run --rpcPort 7000 --pidFile /tmp/agent.pid |tee $output_file
EOF

cat << EOF > $stop_file
pid=\`cat /tmp/agent.pid\`
kill -9 \$pid
EOF
}

gen_cli_master_single_bench()
{
        local bench_type=$1
        local bench_codec=$2
        local bench_name=$3

        local result_name=${bench_type}_${bench_codec}_${bench_name}
        local server_endpoint="http://${bench_app_pub_server}:${bench_app_port}/${bench_config_hub}"

        local customized_connection=$(derefer_2vars $bench_name "connection_number")
        local customized_concurrent=$(derefer_2vars $bench_name "connection_concurrent")
        local customized_send=$(derefer_2vars $bench_name "send_number")
	local customized_send_interval=$(derefer_2vars $bench_name "send_interval")

	local connection_num=$connection_number
	local concurrent_num=$connection_concurrent
	local send_num=$send_number
	local send_interval=1000 # 1000 ms

        if [ "$customized_connection" != "" ]
        then
                connection_num=$customized_connection
        fi
        if [ "$customized_concurrent" != "" ]
        then
                concurrent_num=$customized_concurrent
        fi
        if [ "$customized_send" != "" ]
        then
                send_num=$customized_send
        fi
        if [ "$customized_send_interval" != "" ]
        then
                send_interval=$customized_send_interval
        fi

	local codec=$bench_codec
	if [ $bench_codec == "msgpack" ]
	then
		codec="messagepack"
	fi

cat << EOF > $sigbench_config_dir/${cmd_prefix}_${bench_codec}_${bench_name}_${bench_type}
connection=$connection_num
connection_concurrent=$concurrent_num
send=$send_num
send_interval=$send_interval
EOF

cat << EOF > ${cli_script_prefix}_${result_name}.sh
#!/bin/bash
if [ -e ${result_name} ]
then
        rm -rf ${result_name}
fi

mkdir ${result_name}
if [ -e /tmp/master.pid ]
then
        pid=\`cat /tmp/master.pid\`
        kill -9 \$pid
fi
if [ -e jobResult.txt ]
then
	rm jobResult.txt
fi
transport=${bench_transport}
server=$server_endpoint
pipeline="createConn;startConn;up${send_num};scenario;stopConn;disposeConn"
slaveList="${cli_agents_g}"

/home/${bench_app_user}/.dotnet/dotnet run -- --rpcPort 7000 --duration $sigbench_run_duration --connections $connection_num --interval 1 --serverUrl "\${server}" --pipeLine "\${pipeline}" -v $bench_type -t "\${transport}" -p ${codec} -s ${bench_name} --slaveList "\${slaveList}"	-o ${result_name}/counters.txt --pidFile /tmp/master.pid --concurrentConnection ${concurrent_num}
EOF
}

append_cli_agents_to_global_var()
{
        local server=$1
        local port=$2
        local user=$3
        local localip=${server}
        # trim whitespace
        localip=`echo "$localip"|awk '{$1=$1};1'`
        if [ "$cli_agents_g" != "" ]
        then
                cli_agents_g=${cli_agents_g}";${localip}"
        else
                cli_agents_g="${localip}"
        fi
}

gen_cli_master_bench()
{
  cli_agents_g=""
  iterate_all_bench_server append_cli_agents_to_global_var
  iterate_all_scenarios gen_cli_master_single_bench
}

entry_copy_cli_scripts_to_master()
{
        local servers server port user
        # master node
        servers=$(array_get $bench_server_list 1 $bench_server_sep)
        server=$(array_get $servers 1 $bench_server_inter_sep)
        port=$(array_get $servers 2 $bench_server_inter_sep)
        user=$(array_get $servers 3 $bench_server_inter_sep)
        scp -o StrictHostKeyChecking=no -P $port ${cli_script_prefix}_*.sh ${user}@${server}:${bench_master_folder}/
}

do_single_cli_bench()
{
        local server=$1
        local port=$2
        local user=$3
        local script=$4
        scp -o StrictHostKeyChecking=no -P $port $script ${user}@${server}:${bench_slave_folder}
        ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; chmod +x ./$script"
        nohup ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; ./$script" &
	local end=$((SECONDS + 120))
	while [ $SECONDS -lt $end ]
	do
		scp -o StrictHostKeyChecking=no -P $port ${user}@${server}:${bench_slave_folder}/${cli_bench_agent_output} .
		local check=`grep "started" ${cli_bench_agent_output}`
		if [ "$check" != "" ]
		then
			echo "agent started!"
			break
		else
			echo "waiting for agent started.."
		fi
		sleep 1
	done
}

start_single_cli_bench()
{
	do_single_cli_bench $1 $2 $3 $cli_bench_start_script
}

stop_single_cli_bench()
{
	do_single_cli_bench $1 $2 $3 $cli_bench_stop_script
}

entry_copy_start_cli_bench()
{
	iterate_all_bench_server start_single_cli_bench
}

entry_copy_stop_cli_bench()
{
	iterate_all_bench_server stop_single_cli_bench
}

entry_gen_all_cli_scripts()
{
	gen_cli_master_bench
	gen_cli_agent_bench $cli_bench_start_script $cli_bench_stop_script $cli_bench_agent_output
}

check_cli_master_and_wait()
{
        local flag_file=$1
	local output_log=$2
        local rand=`date +%H%M%S`
        local end=$((SECONDS + $sigbench_run_duration + 60))
        local finish=0
	local master_log=${output_log}_${rand}.txt
        while [ $SECONDS -lt $end ] && [ "$finish" == "0" ]
        do
                # check whether master finished
                finish=`cat $flag_file`
                # check master output
		fail_flag_g=`egrep -i "errors|exception" ${output_log}`
                if [ "$fail_flag_g" != "" ]
                then
			cp ${output_log} $master_log
			echo "master error: '$master_log'"
			echo "Error occurs, please check $master_log"
			mark_error ${master_log}
                        break;
                fi
                #echo "wait benchmark to complete ('$finish')..."
                sleep 1
        done
}

launch_master_cli()
{
        local script_name=$1
        local server=$2
        local port=$3
        local user=$4
        local status_file=$5
        local remote_run="autogen_runclibench.sh"
cat << _EOF > $remote_run
#!/bin/bash
#automatic generated script
echo "0" > $status_file # flag indicates not finish
ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_master_folder}; sh $script_name" 2>&1|tee -a ${result_dir}/${script_name}.log
echo "1" > $status_file # flag indicates finished
_EOF
        nohup sh $remote_run &
	g_master_cli_pid=$!
}

entry_launch_master_cli_script()
{
	clear_error_mark
	iterate_all_scenarios launch_single_master_cli_script
}

launch_single_master_cli_script()
{
        local bench_type=$1
        local bench_codec=$2
        local bench_name=$3

        local result_name=${bench_type}_${bench_codec}_${bench_name}
        local flag_file="cli_master_status.tmp"
        local service_vm_info_file="cpuinfo.txt"
        local servers server port user
        # master node
        servers=$(array_get $bench_server_list 1 $bench_server_sep)
        server=$(array_get $servers 1 $bench_server_inter_sep)
        port=$(array_get $servers 2 $bench_server_inter_sep)
        user=$(array_get $servers 3 $bench_server_inter_sep)

        if [ ! -e ${result_dir}/$result_name ]
        then
                mkdir ${result_dir}/$result_name
        fi

        collect_service_vm_basic_info_if_possible ${result_dir}/$result_name/$service_vm_info_file
        collect_service_cpu_usage_if_possible ${result_dir}/$result_name/$service_vm_info_file
	echo "launch RPC master node"
        launch_master_cli ${cli_script_prefix}_${result_name}.sh $server $port $user $flag_file

        check_cli_master_and_wait $flag_file ${result_dir}/${cli_script_prefix}_${result_name}.sh.log
        if [ "$pid_to_collect_top" != "" ]
        then
                kill $pid_to_collect_top
        fi
	scp -o StrictHostKeyChecking=no -r -P $port ${user}@${server}:${bench_master_folder}/$result_name ${result_dir}/
	if [ "$g_master_cli_pid" != "" ]
	then
		kill $g_master_cli_pid
	fi
}

start_cli_bench_server()
{
. ./servers_env.sh
        local connection_str="$1"
        local output_log=$2
        local local_run_script="auto_local_launch.sh"
        local remote_run_script="auto_launch_app.sh"
cat << _EOF > $remote_run_script
#!/bin/bash
#automatic generated script
killall dotnet
cd ${bench_server_folder} 
export Azure__SignalR__ConnectionString="$connection_str"
/home/${bench_app_user}/.dotnet/dotnet run
_EOF

scp -o StrictHostKeyChecking=no -P ${bench_app_pub_port} $remote_run_script ${bench_app_user}@${bench_app_pub_server}:~/

cat << _EOF > $local_run_script
#!/bin/bash
#automatic generated script
ssh -o StrictHostKeyChecking=no -p ${bench_app_pub_port} ${bench_app_user}@${bench_app_pub_server} "sh $remote_run_script"
_EOF

        nohup sh $local_run_script > ${output_log} 2>&1 &
        local end=$((SECONDS + 60))
        local finish=0
        local check
        while [ $SECONDS -lt $end ] && [ "$finish" == "0" ]
        do
                check=`grep "HttpConnection Started" ${output_log}|wc -l`
                if [ "$check" -ge "5" ]
                then
                        finish=1
                        echo "server is started!"
                        break
                else
                        echo "wait for server starting..."
                fi
                sleep 1
        done
}

stop_cli_bench_server()
{
	. ./servers_env.sh
        ssh -o StrictHostKeyChecking=no -p ${bench_app_pub_port} ${bench_app_user}@${bench_app_pub_server} "killall dotnet"
}
