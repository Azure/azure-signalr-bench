#!/bin/bash
. ./env.sh

function log()
{
	echo "$*"
}

## Bourne shell does not support array, so a string is used
## to work around with the hep of awk array

## return the value according to index: 
## @param arr: array (using string)
## @param index: array index (start from 1)
## @param separator: string's separator which is used separate the array item
array_get() {
  local arr=$1
  local index=$2
  local separator=$3
  echo ""|awk -v sep=$separator -v str="$arr" -v idx=$index '{
   split(str, array, sep);
   print array[idx]
}'
}

## return the length of the array
## @param arr: array (using string)
## @param separator: string's separator which is used separate the array item
array_len() {
  local arr=$1
  local separator=$2
  echo ""|awk -v sep=$separator -v str="$arr" '{
   split(str, array, sep);
   print length(array)
}'
}

## given "echo" and "connection_number"
## return the value of $echoconnection_number
derefer_2vars() {
  local prefix=$1
  local postfix=$2
  local v=${prefix}${postfix}
  eval echo \$${v}
}

derefer_3vars() {
  local head=$1
  local body=$2
  local tail=$3
  local v=${head}${body}${tail}
  eval echo \$${v}
}

function create_root_folder() {
  export result_root=`date +%Y%m%d%H%M%S`
  mkdir $result_root
}

function iterate_all_bench_server() {
	local callback=$1
	local len=$(array_len "$bench_server_list" "$bench_server_sep")
	local i
	local servers server port user
	i=1
	while [ $i -le $len ]
	do
		servers=$(array_get $bench_server_list $i $bench_server_sep)
		server=$(array_get $servers 1 $bench_server_inter_sep)
		port=$(array_get $servers 2 $bench_server_inter_sep)
		user=$(array_get $servers 3 $bench_server_inter_sep)
		$callback ${server} $port ${user} $i
		i=$(($i+1))
	done
}

function iterate_all_scenarios() {
	local callback=$1
	local i j k
	for j in $bench_type_list
	do
		for i in $bench_name_list
		do
			for k in $bench_codec_list
			do
				local result_name=${j}_${k}_${i}
				$callback $j $k $i #selfhost json echo
			done
		done
	done
}

function gen_all_report() {
for i in `ls $result_dir`
do
   if [ -e $result_dir/$i/latency_table_1s_category.js ]
   then
      sed "s/1s_percent_table_div/${i}_1s_percent_table_div/g" $result_dir/$i/latency_table_1s_category.js > $result_dir/${i}_latency_table_1s_category.js
   fi
   if [ -e $result_dir/$i/latency_table_500ms_category.js ]
   then
      sed "s/500ms_percent_table_div/${i}_500ms_percent_table_div/g" $result_dir/$i/latency_table_500ms_category.js > $result_dir/${i}_latency_table_500ms_category.js
   fi
done

. ./servers_env.sh
export BenchEndpoint=${bench_server}:${bench_server_port}
export SignalRServiceExtSSHEndpoint=${bench_service_pub_server}:${bench_service_pub_port}
export SignalRServiceIntEndpoint=${bench_service_server}:${bench_service_port}
export SignalRDemoAppExtSSHEndpoint=${bench_app_pub_server}:${bench_app_pub_port}
export SignalRDemoAppIntEndpoint=${bench_app_server}:${bench_app_port}

python render_tmpl.py -t tmpl/all.html >$result_dir/all.html
}

function gen_summary_body
{
  local html_root=$1
  local output=$2
  local i
  echo "{{define \"body\"}}" > $output
  for i in `ls -t $html_root`
  do
    is_valid_src=`echo $i|awk '{if ($1 ~ /^[+-]?[0-9]+$/) {print 1;} else {print 0;}}'`
    if [ $is_valid_src == 1 ]
    then
	if [ -e $html_root/$i/all.html ]
	then
		echo "    <div><a href=\"${i}/all.html\">${i} all scenarios</a></div>" >> $output
	else
		for j in `ls $html_root/$i`
		do
			if [ -e $html_root/$i/$j/index.html ]
			then
				echo "    <div><a href=\"${i}/${j}/index.html\">$i</a></div>" >> $output
			fi
		done
	fi
    fi
  done
  echo "{{end}}" >> $output
}

function gen_summary() {
	tmp_sum=/tmp/summary_body.tmpl
	gen_summary_body $nginx_root $tmp_sum
	go run gensummary.go -header="tmpl/header.tmpl" -content="tmpl/summary.tmpl" -body="$tmp_sum" -footer="tmpl/footer.tmpl" > ${nginx_root}/summary.html
}

function prepare
{
	if [ ! -e $result_dir ]
	then
		mkdir -p $result_dir
	fi
}

function do_single_sigbench() {
	local server=$1
	local port=$2
	local user=$3
	local script=$4
	scp -o StrictHostKeyChecking=no -P $port $script ${user}@${server}:~/$sigbench_home
	ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd $sigbench_home; chmod +x ./$script"
	ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd $sigbench_home; ./$script"
}

function gen_agent_start_stop_script
{
cat << _EOF > $bench_start_file
#!/bin/bash
. ./${sigbench_env_file}

pid=\`cat /tmp/websocket-bench.pid\`
kill -9 \$pid

nohup ./$sigbench_name > \$agent_output 2>&1 &
_EOF

cat << _EOF > $bench_stop_file
pid=\`cat /tmp/websocket-bench.pid\`
kill -9 \$pid
_EOF
}

function append_agents_to_env_file() {
	local server=$1
	local port=$2
	local user=$3
	#localip=`ssh -p $port ${user}@${server} "hostname -I"`
	#localip=`echo "$localip"|awk '{$1=$1};1'`
	local localip=${server}
	# trim whitespace
	localip=`echo "$localip"|awk '{$1=$1};1'`
	if [ "$agents_g" != "" ]
	then
		agents_g=${agents_g}",${localip}:7000"
	else
		agents_g="${localip}:7000"
	fi
}


function gen_benchmark_impls_name()
{
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	local g_bench_env_file="/tmp/autogen_bench_env.sh"
cat << _EOF >> $g_bench_env_file
${bench_type}_${bench_codec}_${bench_name}=signalr:service:${bench_codec}:${bench_name}
_EOF
}

function gen_websocket_bench_env()
{
	local bench_env=$1
cat << _EOF > $bench_env
#automatic generated file
# for agents
agent_output=$sigbench_agent_output
_EOF
	local g_bench_env_file="/tmp/autogen_bench_env.sh"
	echo "# for master" > $g_bench_env_file
	iterate_all_scenarios gen_benchmark_impls_name
	cat $g_bench_env_file >> $bench_env

	agents_g=""
	iterate_all_bench_server append_agents_to_env_file
cat << _EOF >> $bench_env
agents="$agents_g"
_EOF
}

function gen_single_cmd_file() {
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	local cmd_prefix="cmd_4"
	
	. $sigbench_config_dir/${cmd_prefix}_${bench_codec}_${bench_name}_${bench_type}
cat << EOF > ${cmd_file_prefix}_${bench_codec}_${bench_name}_${bench_type}
c $connection $connection_concurrent
s $send $send_interval
wr
w $sigbench_run_duration
EOF
}

function update_jenkins_command_configs()
{
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	local cmd_prefix="cmd_4"
	# echo and broadcast have different connection_number, concurrent_connection_number and send_number
	# we first find connection_number, connection_concurrent, and send_number from customized setting,
	# if they are empty, then we fallback to default values
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
cat << EOF > $sigbench_config_dir/${cmd_prefix}_${bench_codec}_${bench_name}_${bench_type}
connection=$connection_num
connection_concurrent=$concurrent_num
send=$send_num
send_interval=$send_interval
EOF
}

function gen_jenkins_command_config()
{
	iterate_all_scenarios update_jenkins_command_configs
}

function gen_cmd_files()
{
	iterate_all_scenarios gen_single_cmd_file
}

function start_single_sigbench
{
	do_single_sigbench $1 $2 $3 $bench_start_file
}

function stop_single_sigbench
{
	do_single_sigbench $1 $2 $3 $bench_stop_file
}

function start_sigbench() {
	iterate_all_bench_server start_single_sigbench
}

function stop_sigbench() {
	iterate_all_bench_server stop_single_sigbench
}

function launch_websocket_master
{
	local script_name=$1
	local server=$2
	local port=$3
	local user=$4
	local status_file=$5
	local remote_run="autogen_runbench.sh"
cat << _EOF > $remote_run
#!/bin/bash
#automatic generated script
echo "0" > $status_file # flag indicates not finish
ssh -o StrictHostKeyChecking=no -p $port ${user}@${server} "cd $sigbench_home; sh $script_name" 2>&1|tee -a ${result_dir}/${script_name}.log
echo "1" > $status_file # flag indicates finished
_EOF
	nohup sh $remote_run &
	g_web_master_pid=$!
}

function check_single_agent() {
	local server=$1
	local port=$2
	local user=$3
	local idx=$4
	local rand=`date +%H%M%S`
	local agent_log=${result_dir}/${idx}_${rand}_${sigbench_agent_output}

	if [ "$fail_flag_g" != "" ]
	then
		# already encounter error
		return
	fi
	scp -o StrictHostKeyChecking=no -P $port ${user}@${server}:~/$sigbench_home/$sigbench_agent_output ${agent_log} > /dev/null 2>&1
	fail_flag_g=`egrep -i "fail|error" ${agent_log}`
	if [ "$fail_flag_g" != "" ]
	then
		echo "agent error: '$fail_flag_g'"
		echo "Error occurs, so break the benchmark, please check ${agent_log}"
		mark_error ${agent_log}
	fi
}

function check_and_wait
{
	local flag_file=$1
	local end=$((SECONDS + $sigbench_run_duration))
	local finish=0
	while [ $SECONDS -lt $end ] || [ "$finish" == "0" ]
	do
		# check whether master finished
		finish=`cat $flag_file`
		# check all agents output
		iterate_all_bench_server check_single_agent
		if [ "$fail_flag_g" != "" ]
		then
			break;
		fi
		#echo "wait benchmark to complete ('$finish')..."
		sleep 1
	done
}

function mark_error() {
	local err_file=$1
	local mark_err_file=${result_dir}/$error_mark_file
	cat $err_file >> ${mark_err_file}
	echo "Mark the error in ${mark_err_file}"
}

function clear_error_mark() {
	if [ -e ${result_dir}/$error_mark_file ]
	then
		rm ${result_dir}/$error_mark_file
	fi
}

collect_service_vm_basic_info_if_possible() {
	local output=$1
	if [ "$bench_service_pub_server" != "" ] && [ "$bench_service_pub_port" != "" ] && [ "$bench_service_user" != "" ]
	then
		echo "=====================Accelerated Networking Check=======================" > $output
		ssh -o StrictHostKeyChecking=no -p${bench_service_pub_port} ${bench_service_user}@${bench_service_pub_server} "lspci" >> $output
		ssh -o StrictHostKeyChecking=no -p${bench_service_pub_port} ${bench_service_user}@${bench_service_pub_server} "ethtool -S eth0 | grep vf_" >> $output
		echo "=====================CPU Info=======================" >> $output
		ssh -o StrictHostKeyChecking=no -p${bench_service_pub_port} ${bench_service_user}@${bench_service_pub_server} "lscpu" >> $output
	fi
}

collect_service_cpu_usage_if_possible() {
	local output=$1
        if [ "$bench_service_pub_server" != "" ] && [ "$bench_service_pub_port" != "" ] && [ "$bench_service_user" != "" ]
        then
		nohup sh collect_top.sh $bench_service_pub_server $bench_service_pub_port $bench_service_user $output &
		pid_to_collect_top=$!
	fi
}

function run_single_master_script_and_check() {
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	
	local result_name=${bench_type}_${bench_codec}_${bench_name}
	local flag_file="master_status.tmp"
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
 
	launch_websocket_master ${websocket_script_prefix}_${result_name}.sh $server $port $user $flag_file

	check_and_wait $flag_file
	if [ "$pid_to_collect_top" != "" ]
	then
		kill $pid_to_collect_top
	fi
	if [ "$g_web_master_pid" != "" ]
	then
		kill $g_web_master_pid
	fi
	# fetch result
	scp -o StrictHostKeyChecking=no -r -P $port ${user}@${server}:~/$sigbench_home/$result_name ${result_dir}/
}

function gen_single_websocket_script() {
	local bench_type=$1
	local bench_codec=$2
	local bench_name=$3
	
	local result_name=${bench_type}_${bench_codec}_${bench_name}
	local server_endpoint=${bench_app_pub_server}:${bench_app_port}/${bench_config_hub}

	local wss_option
	if [ $use_https == "1" ]
	then
		wss_option="-u"
	else
		wss_option=""
	fi

cat << EOF > ${websocket_script_prefix}_${result_name}.sh
#auto generated file
. ./$sigbench_env_file
if [ -e ${result_name} ]
then
	rm -rf ${result_name}
fi

if [ -e /tmp/websocket-bench-master.pid ]
then
	pid=\`cat /tmp/websocket-bench-master.pid\`
	kill -9 \$pid
fi
EOF
	if [ "$bench_send_size" == "0" ]
	then
cat << EOF >> ${websocket_script_prefix}_${result_name}.sh
./websocket-bench -m master -a "\${agents}" -s "${server_endpoint}" -t \$${result_name} -c ${cmd_file_prefix}_${bench_codec}_${bench_name}_${bench_type} -o ${result_name} ${wss_option}
EOF
	else
cat << EOF >> ${websocket_script_prefix}_${result_name}.sh
./websocket-bench -m master -a "\${agents}" -s "${server_endpoint}" -t \$${result_name} -c ${cmd_file_prefix}_${bench_codec}_${bench_name}_${bench_type} -o ${result_name} ${wss_option} -b $bench_send_size
EOF
	fi
}

function launch_all_websocket_scripts()
{
	clear_error_mark
	iterate_all_scenarios run_single_master_script_and_check
}

function gen_websocket_scripts
{
	iterate_all_scenarios gen_single_websocket_script
}

function gen_websocket_bench
{
	gen_websocket_bench_env $sigbench_env_file
	## generate command files
	gen_cmd_files

	gen_websocket_scripts

	gen_agent_start_stop_script
}

function copy_scripts_to_all_server()
{
	local server=$1
	local port=$2
	local user=$3
	scp -o StrictHostKeyChecking=no -P $port $sigbench_env_file ${user}@${server}:~/${sigbench_home}/
}

function copy_scripts_to_master()
{
	local servers server port user
	# master node
	servers=$(array_get $bench_server_list 1 $bench_server_sep)
	server=$(array_get $servers 1 $bench_server_inter_sep)
	port=$(array_get $servers 2 $bench_server_inter_sep)
	user=$(array_get $servers 3 $bench_server_inter_sep)
	scp -o StrictHostKeyChecking=no -P $port ${websocket_script_prefix}_*.sh ${user}@${server}:~/${sigbench_home}/
	scp -o StrictHostKeyChecking=no -P $port ${cmd_file_prefix}* ${user}@${server}:~/${sigbench_home}/
}

function deploy_all_scripts_config_bench()
{
	copy_scripts_to_master
	iterate_all_bench_server copy_scripts_to_all_server
}

function start_sdk_server()
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
cd /home/${bench_app_user}/signalr-bench/AzureSignalRChatSample/ChatSample
export Azure__SignalR__ConnectionString="$connection_str"
/home/${bench_app_user}/.dotnet/dotnet restore --no-cache # never use cache library
/home/${bench_app_user}/.dotnet/dotnet run
_EOF

scp -o StrictHostKeyChecking=no -P ${bench_app_pub_port} $remote_run_script ${bench_app_user}@${bench_app_pub_server}:~/

cat << _EOF > $local_run_script
#!/bin/bash
#automatic generated script
ssh -o StrictHostKeyChecking=no -p ${bench_app_pub_port} ${bench_app_user}@${bench_app_pub_server} "sh $remote_run_script"
_EOF

        nohup sh $local_run_script > ${output_log} 2>&1 &

	local end=$((SECONDS + 120))
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

function stop_sdk_server()
{
. ./servers_env.sh
	ssh -o StrictHostKeyChecking=no -p ${bench_app_pub_port} ${bench_app_user}@${bench_app_pub_server} "killall dotnet"
}

function extract_servicename_from_connectionstring() {
	local connectionString=$1
	local is_dogfood=`echo "$connectionString"|grep "servicedev.signalr.net"`
	if [ "$is_dogfood" != "" ]
	then
		local serviceName=`echo "$connectionString"|awk -F = '{print $2}'|awk -F ";" '{print $1}'|awk -F '//' '{print $2}'|awk -F . '{print $1}'`
		if [ "$serviceName" != "" ]
		then
			echo "$serviceName"
			return
		fi
	fi
	echo ""
}

function gen_final_report() {
  sh gen_all_tabs.sh
  sh publish_report.sh
  sh gen_summary.sh # refresh summary.html in NginxRoot gen_summary
  sh send_mail.sh $HOME/NginxRoot/$result_root/allunits.html
}
