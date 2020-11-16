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

verify_accel_network() {
  local host_list="$1"
  local ssh_user="$2"
  local ssh_port="$3"
  local host
  local len=$(array_len $host_list "|")
  local i
  i=1
  while [ $i -le $len ]
  do
    host=$(array_get "$host_list" $i "|")
    echo "ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host}"
    ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host} "lspci"
    ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host} "ethtool -S eth0 | grep vf_"
    i=$(($i+1))
  done
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
	local dir=`dirname $0`
	. $dir/${cmd_prefix}_${bench_codec}_${bench_name}_${bench_type}
cat << EOF > ${cmd_file_prefix}_${bench_codec}_${bench_name}_${bench_type}
c $connection $connection_concurrent
s $send $send_interval
wr
w $sigbench_run_duration
EOF
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


function isDogfood() {
	local connectionString=$1
        local is_dogfood=`echo "$connectionString"|grep "servicedev.signalr.net"`
	if [ "$is_dogfood" != "" ]
	then
		echo 1
	else
		echo 0
	fi
}

function extract_servicename_from_connectionstring() {
	local connectionString=$1
	local is_dogfood=`echo "$connectionString"|grep "servicedev.signalr.net"`
        local is_prod=`echo "$connectionString"|grep "service.signalr.net"`
	if [ "$is_dogfood" != "" ] || [ "$is_prod" != "" ]
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

function record_build_info() {
  cat << EOF > /tmp/send_mail.txt
Jenkins job details: $BUILD_URL/console
EOF
}


function gen_final_report() {
  #sh gen_all_tabs.sh
  if [ "$kind" == "" ]
  then
    gen_all_tabs_4_report $result_dir ${html_dir} "1s_percent_table_div" latency_table_1s_category.js "1s latency"
  else
    gen_all_tabs_4_report $result_dir ${html_dir} "tab_for_sum" connect_stat_sum.js "connection stat"
  fi
  sh publish_report.sh
  sh gen_summary.sh # refresh summary.html in NginxRoot gen_summary
  sh gen_asrs_warns.sh $nginx_root $result_root
  sh gen_asrs_health_stat.sh $nginx_root $result_root
  sh gen_appserver_exception.sh $nginx_root $result_root
  sh gen_nginx_error.sh $nginx_root $result_root
  sh send_mail.sh $nginx_root/$result_root/allunits.html
  echo "final report: http://$nginx_server_dns/$result_root/allunits.html"
}

iterate_all_vms() {
  local vm_list="$1"
  local user=$2
  local port=$3
  local callback=$4
  local appendix=""
  local i len vm_host
  if [ $# -eq 5 ]
  then
     appendix="$5"
  fi
  len=$(array_len $vm_list "|")
  i=1
  while [ $i -le $len ]
  do
    vm_host=$(array_get "$vm_list" $i "|")
    $callback $vm_host $user $port $appendix
    i=$(($i+1))
  done
}

function stop_collect_top_on_single_vm() {
  local output_dir=$1
  for i in `ls $output_dir/top_nohup_pid_*`
  do
    local pid=`cat $i`
    kill -9 $pid
  done
}

function collect_top_on_single_vm() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  local output_folder=$4
  local random=`tr -cd '[:alnum:]' < /dev/urandom | fold -w4 | head -n1`
  local output_file="top_${random}_${vm_host}.txt"
  local bg_pid_file="top_nohup_pid_${random}"
  local script="/tmp/nohup_top_${random}.sh"
cat << EOF > $script
#!/bin/bash
while [ 1 ]
do
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "top -b|head -n 10" >> $output_folder/$output_file
  sleep 1
done
EOF
  chmod +x $script
  nohup $script &
  echo $! > $output_folder/$bg_pid_file
}

function collect_top_on_all_vms() {
  local vm_list="$1"
  local user=$2
  local port=$3
  local output_folder=$4
  iterate_all_vms "$vm_list" $user $port collect_top_on_single_vm $output_folder
}

function check_single_service_client_connection() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  #local client_conn=`ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "curl http://localhost:5003/health/stat"`
  local client_conn=`ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "netstat -an|grep :5001|grep EST|wc -l"`
  echo "${vm_host}: ${client_conn}"
}

function check_all_service_client_connection() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port check_single_service_client_connection
}

function check_single_vm_ssh() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  #local client_conn=`ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "curl http://localhost:5003/health/stat"`
  nc -z -w5 $vm_host $ssh_port
  if [ $? -ne 0 ]
  then
    echo "SSH on $vm_host is not ready"
  else
    ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "exit"
  fi
  #local client_conn=`ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "netstat -an|grep :5001|grep EST|wc -l"`
}

function check_all_vm_ssh() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port check_single_vm_ssh
}

function get_ntpq_stat_on_single_vm() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  echo "=======================$vm_host================="
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "ntpq -np"
}

function get_ntpq_stat_on_all_vm() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port get_ntpq_stat_on_single_vm
}

function force_sync_time_on_single_vm() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  echo "=======================$vm_host================="
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "sudo service ntp stop;sudo ntpd -gq;sudo service ntp start;sudo ntpq -np"
}

function force_sync_time_on_all_vm() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port force_sync_time_on_single_vm
}


function install_ntp_on_single_vm() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "sudo apt-get install -y ntp ntpstat"
}

function install_ntp_on_all_vm() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port install_ntp_on_single_vm
}

disable_exit_immediately_when_fail()
{
  set +e
}

enable_exit_immediately_when_fail()
{
  set -e
}

gen_all_tabs_4_report() {
  local in_dir=$1
  local out_dir=$2
  local html_id=$3
  local js_tgt_file=$4
  local desc="$5"
  local jobName=`echo "${JOB_NAME}"|tr ' ' '_'`
  local postfix=${jobName}`date +%Y%m%d%H%M%S`
  local tmp_tabs=/tmp/tabs_${postfix}
  local js_refer_tmpl_file=/tmp/tabs_js_refer_${postfix}
  local i j

  echo "{{define \"allunitsjs\"}}" > $js_refer_tmpl_file

  for i in `ls $in_dir`
  do
    if [ -e $in_dir/$i/${js_tgt_file} ]
    then
      sed "s/${html_id}/${i}_${html_id}/g" $in_dir/$i/${js_tgt_file} > $in_dir/${i}_${js_tgt_file}
      echo "   <script type='text/javascript' src='${i}_${js_tgt_file}'></script>" >> $js_refer_tmpl_file
      echo $i|awk -F _ '{print $1}' >>$tmp_tabs
    fi
  done
  echo "{{end}}" >> $js_refer_tmpl_file

  local tmp_tabs_tmpl=/tmp/tabs_tmpl_${postfix}
  local tmp_tabs_tmpl_single=/tmp/tabs_tmpl_single_${postfix}
  local tabs_list_gen=/tmp/tabs_list_tmpl_${postfix}
  local tabs_tmpl_gen=/tmp/tabs_content_tmpl_${postfix}
 
  echo "{{define \"tablist\"}}" > $tabs_list_gen
  echo "{{define \"tabcontents\"}}" > $tabs_tmpl_gen
  for i in `sort $tmp_tabs|uniq`
  do
    echo "                <li><a href='#${i}'>${i}</a></li>" >>$tabs_list_gen

    echo "{{define \"tabcontentlist\"}}" > $tmp_tabs_tmpl_single
    for j in $in_dir/${i}_*
    do
      if [ -e $j/${js_tgt_file} ]
      then
        local item=`echo $j|awk -F / '{print $2}'`
        echo "                                <li><a href='$item/index.html'>$item $desc</a><div id='${item}_${html_id}'></div></li>" >> $tmp_tabs_tmpl_single
      fi
    done
    echo "{{end}}" >> $tmp_tabs_tmpl_single
    export TabID="$i"
    export TabHeadline="$i $desc"
    go run gentabcontent.go -content=tmpl/tabitem.tmpl -tabcontentlist=$tmp_tabs_tmpl_single > $tmp_tabs_tmpl
    cat $tmp_tabs_tmpl >> $tabs_tmpl_gen
  done
  echo "{{end}}" >> $tabs_tmpl_gen
  echo "{{end}}" >> $tabs_list_gen

  go run gen5tmpl.go -content=tmpl/alltabs.tmpl -t1=tmpl/header.tmpl -t2=${js_refer_tmpl_file} -t3=$tabs_list_gen -t4=$tabs_tmpl_gen > $out_dir/allunits.html
  rm /tmp/tabs*${postfix}
}

function export_sql_mgr_env()
{
  local wrkDir=`pwd`/tools/ReportToDB
  local sqlConnStr=`cat sqlconnectionstring.txt`
  export SQL_CLI_DIR=$wrkDir
  export SQL_CONNSTR=$sqlConnStr
  export DEFAULT_SQL_TBL="AzureSignalRPerf"
  export DEFAULT_SQL_CONN_STAT_TBL="AzureSignalRLongrun"
  local rootDir=`pwd`/..
  find $rootDir -iname obj|xargs rm -rf # clean objs to avoid build errors
}

function drop_sql_perf_table()
{
  local SQL_PERF_TBL=$1
  export_sql_mgr_env
  cd $SQL_CLI_DIR
  dotnet run -- dropTable --SqlConnectionString "$SQL_CONNSTR" --TableName $SQL_PERF_TBL
  cd -
}

function insert_records_to_perf_table()
{
  local tblFile tblName
  export_sql_mgr_env
  tblName=$DEFAULT_SQL_TBL
  if [ $# -eq 1 ]
  then
    tblFile=$1
  else
    if [ $# -eq 2 ]
    then
      tblFile=$1
      tblName=$2
    fi
  fi
  cd $SQL_CLI_DIR
  dotnet run -- insertRecords --SqlConnectionString "$SQL_CONNSTR" --TableName $tblName --InputFile $tblFile
  cd -
}

function insert_longrun_records_to_perf_table()
{
  local tblFile tblName
  export_sql_mgr_env
  tblName=$DEFAULT_SQL_CONN_STAT_TBL
  if [ $# -eq 1 ]
  then
    tblFile=$1
  else
    if [ $# -eq 2 ]
    then
      tblFile=$1
      tblName=$2
    fi
  fi
  cd $SQL_CLI_DIR
  dotnet run -- insertRecords --SqlConnectionString "$SQL_CONNSTR" --TableType 2 --TableName $tblName --InputFile $tblFile
  cd -
}
