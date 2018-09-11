#!/bin/bash
. ./func_env.sh

bench_slave_folder=/home/${bench_app_user}/azure-signalr-bench/v2/Rpc/Bench.Server
bench_master_folder=/home/${bench_app_user}/azure-signalr-bench/v2/Rpc/Bench.Client
bench_server_folder=/home/${bench_app_user}/azure-signalr-bench/v2/AppServer
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
        local server_endpoint
	local i
	local app_server_list=""
	local server
	local secenario=$bench_name
	local sendToFixClient_option=""
	if [ "$bench_name" == "sendToFixClient" ]
	then
		sendToFixClient_option="--sendToFixedClient true"
		secenario="sendToClient"
	fi
	local len=$(array_len $bench_app_pub_server "|")
	if [ $len == 1 ]
	then
		server_endpoint="http://${bench_app_pub_server}:${bench_app_port}/${bench_config_hub}"
	else
		i=1
		while [ $i -le $len ]
		do
			server=$(array_get "$bench_app_pub_server" $i "|")
			if [ "$app_server_list" == "" ]
			then
				app_server_list="http://${server}:${bench_app_port}/${bench_config_hub}"
			else
				app_server_list="${app_server_list};http://${server}:${bench_app_port}/${bench_config_hub}"
			fi
			i=$(($i+1))
		done
		server_endpoint="$app_server_list"
	fi

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

	local send_size="2k" #defualt send size
	if [ "$bench_send_size" != "" ]
	then
		send_size=$bench_send_size
	fi

cat << EOF > $sigbench_config_dir/${cmd_config_prefix}_${bench_codec}_${bench_name}_${bench_type}
connection=$connection_num
connection_concurrent=$concurrent_num
send="$send_num"
send_interval=$send_interval
EOF
	local pipeline=""
	local groupOption=""
	if [ $bench_name == "SendGroup" ]
	then
		pipeline="createConn;startConn;joinGroup;${send_num}leaveGroup;stopConn;disposeConn"
		groupOption="--groupNum ${group_number} --groupOverlap 1"
	else
		pipeline="createConn;startConn;${send_num}stopConn;disposeConn"
	fi
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
server="$server_endpoint"
pipeline="$pipeline"
slaveList="${cli_agents_g}"
sendSize="${send_size}"
scenario="$secenario"

/home/${bench_app_user}/.dotnet/dotnet run -- --rpcPort 7000 --duration $sigbench_run_duration --connections $connection_num --interval 1 --serverUrl "\${server}" --pipeLine "\${pipeline}" -v $bench_type -t "\${transport}" -p ${codec} -s "\${scenario}" --slaveList "\${slaveList}" -o ${result_name}/counters.txt --pidFile /tmp/master.pid --concurrentConnection ${concurrent_num} --messageSize "\${sendSize}" ${sendToFixClient_option} ${groupOption}
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

do_start_single_cli_bench()
{
        local server=$1
        local port=$2
        local user=$3
        local script=$4
        scp -o StrictHostKeyChecking=no -P $port $script ${user}@${server}:${bench_slave_folder}
        ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; chmod +x ./$script"
        nohup ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; ./$script" &
	local end=$((SECONDS + 120))
	local cli_log="cli_agent_${server}.log"
	while [ $SECONDS -lt $end ]
	do
		scp -o StrictHostKeyChecking=no -P $port ${user}@${server}:${bench_slave_folder}/${cli_bench_agent_output} $cli_log
		local check=`grep "started" ${cli_log}`
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

do_stop_single_cli_bench()
{
        local server=$1
        local port=$2
        local user=$3
        local script=$4
        local rand=`date +%H%M%S`
        local agent_file_name=${server}_${rand}_${cli_bench_agent_output}
        scp -o StrictHostKeyChecking=no -P $port $script ${user}@${server}:${bench_slave_folder}
        ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; chmod +x ./$script"
        ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd ${bench_slave_folder}; ./$script"
        echo "agent stoped!"
	scp -o StrictHostKeyChecking=no -P $port ${user}@${server}:${bench_slave_folder}/${cli_bench_agent_output} ${result_dir}/$agent_file_name
}

start_single_cli_bench()
{
	do_start_single_cli_bench $1 $2 $3 $cli_bench_start_script
}

stop_single_cli_bench()
{
	do_stop_single_cli_bench $1 $2 $3 $cli_bench_stop_script
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
        sh $remote_run # RPC master exit when it finished
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

	echo "Finish running all"
        if [ "$pid_to_collect_top" != "" ]
        then
                kill -9 $pid_to_collect_top
        fi
	scp -o StrictHostKeyChecking=no -r -P $port ${user}@${server}:${bench_master_folder}/$result_name ${result_dir}/
}

iterate_all_app_server_and_connection_str()
{
	local connection_string_list="$1"
	local app_server_list="$2"
	local callback=$3
	local ssh_user=$4
	local ssh_port=$5
	local output_log_dir=""
	if [ $# -ne 5 ]
	then
	  output_log_dir="$6"
	fi
	local conn_str_len=$(array_len "$connection_string_list" "|")
	local app_server_len=$(array_len "$app_server_list" "|")
	if [ "$conn_str_len" -gt "$app_server_len" ]
	then
		echo "connection string items ($conn_str_len) are larger than app server ($app_server_len), it means there are some connection strings cannot be running"
		exit 1
	fi
	local i=1
	local app_server
	local conn_str
	while [ $i -le $conn_str_len ]
	do
		conn_str=$(array_get "$connection_string_list" $i "|")
		app_server=$(array_get "$app_server_list" $i "|")
		output_log="app_log_${i}_${app_server}.log"
		$callback "$app_server" $ssh_user $ssh_port "$conn_str" "$output_log_dir"
		i=$(($i+1))
	done
}

start_collect_top_on_app_server()
{
	local app_server_list="$1"
	local ssh_user=$2
	local ssh_port=$3
	local output_dir="$4"
	local app_server_len=$(array_len "$app_server_list" "|")
	local i=1
        local app_server
	local outfile
	local pidfile
	while [ $i -le $app_server_len ]
	do
		app_server=$(array_get "$app_server_list" $i "|")
		outfile=${app_server}_top.txt
		pidfile=${app_server}_pid.txt
		nohup sh collect_top.sh "$app_server" $ssh_port $ssh_user "${output_dir}/$outfile" &
		echo $! > ${output_dir}/$pidfile
		i=$(($i+1))
	done
}

stop_collect_top_on_app_server()
{
	local output_dir=$1
	local pid
	for i in `ls ${output_dir}/*_pid.txt`
	do
		pid=`cat $i`
		kill $pid
	done
}

start_multiple_app_server_with_single_service()
{
	local conn_str="$1"
	local app_server_list="$2"
	local ssh_user=$3
	local ssh_port=$4
	local output_dir="$5"
	local app_server_len=$(array_len "$app_server_list" "|")
	local i=1
        local app_server
	while [ $i -le $app_server_len ]
	do
		app_server=$(array_get "$app_server_list" $i "|")
		start_single_app_server "$app_server" $ssh_user $ssh_port "$conn_str" "$output_dir"
		i=$(($i+1))
	done
}

start_multiple_app_server()
{
	local conn_str_list="$1"
	local app_server_list="$2"
	local ssh_user=$3
	local ssh_port=$4
	local output_dir="$5"
	iterate_all_app_server_and_connection_str "$conn_str_list" "$app_server_list" start_single_app_server $ssh_user $ssh_port "$output_dir"
}

stop_multiple_app_server()
{
	local app_server_list="$1"
	local ssh_user=$2
	local ssh_port=$3
	iterate_all_vms "$app_server_list" $ssh_user $ssh_port stop_single_app_server
}

start_single_app_server()
{
	local app_server="$1"
	local app_user=$2
	local app_ssh_port=$3
	local connection_str="$4"
	local output_log="$5/${app_server}_${app_running_log}"
        local local_run_script="auto_local_launch.sh"
        local remote_run_script="auto_launch_app.sh"
	local useLocalSignalr=$g_use_local_signalr
	if [ "$useLocalSignalr" == "true" ]
	then
cat << _EOF > $remote_run_script
#!/bin/bash
#automatic generated script
killall dotnet
cd ${bench_server_folder}
/home/${bench_app_user}/.dotnet/dotnet restore --no-cache # never use cache library
export useLocalSignalR="true"
/home/${app_user}/.dotnet/dotnet run # >out.log
_EOF
	else
cat << _EOF > $remote_run_script
#!/bin/bash
#automatic generated script
killall dotnet
cd ${bench_server_folder}
/home/${bench_app_user}/.dotnet/dotnet restore --no-cache # never use cache library
/home/${app_user}/.dotnet/dotnet user-secrets set Azure:SignalR:ConnectionString "$connection_str"
/home/${app_user}/.dotnet/dotnet run # >out.log
_EOF
	fi

echo "scp -o StrictHostKeyChecking=no -P ${app_ssh_port} $remote_run_script ${app_user}@${app_server}:~/"
scp -o StrictHostKeyChecking=no -P ${app_ssh_port} $remote_run_script ${app_user}@${app_server}:~/

cat << _EOF > $local_run_script
#!/bin/bash
#automatic generated script
ssh -o StrictHostKeyChecking=no -p ${app_ssh_port} ${app_user}@${app_server} "sh $remote_run_script"
_EOF
        nohup sh $local_run_script > ${output_log} &
        local end=$((SECONDS + 120))
        local finish=0
        local check
        while [ $SECONDS -lt $end ] && [ "$finish" == "0" ]
        do
		#echo "scp -o StrictHostKeyChecking=no -P ${app_ssh_port} ${app_user}@${app_server}:${bench_server_folder}/out.log ${output_log}"
		#scp -o StrictHostKeyChecking=no -P ${app_ssh_port} ${app_user}@${app_server}:${bench_server_folder}/out.log ${output_log}
		if [ "$g_use_local_signalr" == "true" ]
		then
		  check=`grep "Application started" ${output_log}`
		  if [ "$check" != "" ]
		  then
			finish=1
			echo "server is started!"
			return
		  else
			echo "wait for server starting..."
		  fi
		else
                  check=`grep "HttpConnection Started" ${output_log}|wc -l`
                  if [ "$check" != "" ]
                  then
                        finish=1
                        echo "server is started!"
                        return
                  else
                        echo "wait for server starting..."
                  fi
		fi
                sleep 1
        done
	echo "!!Fail server does not start!!"
	exit 1
}

stop_single_app_server()
{
	local app_server=$1
	local ssh_user=$2
	local ssh_port=$3
	ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${app_server} "killall dotnet"
}

start_cli_bench_server()
{
	local connection_str="$1"
	local output_dir=$2
. ./servers_env.sh
	start_single_app_server $bench_app_pub_server $bench_app_user $bench_app_pub_port "$connection_str" $output_dir
}

stop_cli_bench_server()
{
	. ./servers_env.sh
	stop_single_app_server ${bench_app_pub_server} ${bench_app_user} ${bench_app_pub_port}
}
