#!/bin/bash
. ./func_env.sh
g_service_runtime=Microsoft.Azure.SignalR.ServiceRuntime
g_bin_folder_name="ASRS"

function build_signalr_service() {
  local SignalRRootInDir=$1
  local OutDir=$2
  local commit_hash_file="$3"
  cd $SignalRRootInDir/src/${g_service_runtime}
  pwd
  git log -n 1 --pretty=format:"%H" > "$commit_hash_file"
  dotnet restore --no-cache
  dotnet publish -c Release -f netcoreapp2.1 -o "$OutDir" -r linux-x64 --self-contained
  cd -
}

# input:
#  output folder of ASRS Bin
#  Redis connection string
#  host vm list
function deploy_package_4_multiple_service_vm() {
  local raw_bin_dir=$1
  local redis_str="$2"
  local host_list="$3"
  local user=$4
  local port=$5
  local tmp_folder="/tmp"
  local out_dir_name=$g_bin_folder_name
  local tmp_out_dir=${tmp_folder}/${out_dir_name}
  local i len vm_host
  local uuid=`cat /proc/sys/kernel/random/uuid`
  local appsettings_tmpl="servicetmpl/appsettings_redis.json"
  local redisConnStrPlaceholder="RedisConnectionString"
  len=$(array_len $host_list "|")
  i=1
  while [ $i -le $len ]
  do
    vm_host=$(array_get "$host_list" $i "|")
    # modify appsettings.json
    if [ -e $tmp_out_dir ]
    then
       rm -rf $tmp_out_dir
    fi
    cp -r $raw_bin_dir $tmp_out_dir
    sed -e "s/localhost/$vm_host/g" -e "s/dev/$uuid/g" -e "s/$redisConnStrPlaceholder/${redis_str}/g" $appsettings_tmpl > $tmp_out_dir/appsettings.json
    # pack
    cd $tmp_folder
    tar zcvf ${out_dir_name}.tgz $out_dir_name
    cd -
    # deploy
    scp -o StrictHostKeyChecking=no -P $port ${tmp_folder}/${out_dir_name}.tgz ${user}@${vm_host}:~/
    i=$(($i+1))
  done
}

update_single_azure_signalr_bench_appserver() {
  local host=$1
  local ssh_user=$2
  local ssh_port=$3
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host} "cd /home/${ssh_user}/azure-signalr-bench; git pull; cd v2/AppServer; /home/${ssh_user}/.dotnet/dotnet build"
}

update_azure_signalr_bench_appserver() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port update_single_azure_signalr_bench_appserver
}

update_single_azure_signalr_bench_client() {
  local host=$1
  local ssh_user=$2
  local ssh_port=$3
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host} "cd /home/${ssh_user}/azure-signalr-bench; git pull; cd v2/Rpc/Bench.Server; /home/${ssh_user}/.dotnet/dotnet build"
}

check_single_bench_git_log() {
  local host=$1
  local ssh_user=$2
  local ssh_port=$3
  local log=`ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${host} "cd /home/${ssh_user}/azure-signalr-bench; git log | head -n 5"`
  echo "$host"
  echo "-----"
  echo "$log"
}

check_bench_git_log() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port check_single_bench_git_log 
}

update_azure_signalr_bench_client() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port update_single_azure_signalr_bench_client
}

function replace_appsettings() {
  local dir=$1
  local serviceHost=$2
  local appsetting=$3
  sed "s/localhost/$serviceHost/g" $appsetting > $dir/appsettings.json
}

function zip_signalr_service() {
  local dir=$1
  tar zcvf ${dir}.tgz $dir
}

function gen_connection_string_list_from_multiple_service() {
  local vm_list="$1"
  local i len vm_host
  local conn_str_list=""
  local conn_str
  len=$(array_len $vm_list "|")
  i=1
  while [ $i -le $len ]
  do
    vm_host=$(array_get "$vm_list" $i "|")
    conn_str=$(gen_connection_string_from_host $vm_host)
    if [ "$conn_str_list" == "" ]
    then
       conn_str_list="$conn_str"
    else
       conn_str_list="${conn_str_list}|$conn_str"
    fi
    i=$(($i+1))
  done
  echo "$conn_str_list"
}

function gen_connection_string_from_host() {
  local hostname=$1
  echo "Endpoint=http://$hostname;AccessKey=ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
}

function check_build_status() {
  local dir=$1
  if [ -e $dir/${g_service_runtime} ]
  then
    echo 0
  else
    echo 1
  fi
}

function check_service_launch_status() {
  local output_log=$1 
  local check=`grep "Now listening on:" ${output_log}|wc -l`
  local started=`grep "Application started" ${output_log}`
  if [ "$check" -eq "3" ] && [ "$started" != "" ]
  then
    echo 0
  else
    echo 1
  fi
}

build_ASRS_package() {
 local dir=`pwd`
 local outdir=$1
 local hostname=$2
 local appsetting=$3
 local src_root_dir=$4
 local commit_hash_file="$5"

 build_signalr_service $src_root_dir "$dir"/$outdir "$commit_hash_file"

 replace_appsettings $outdir $hostname $appsetting

 zip_signalr_service $outdir
}

function build_and_launch() {
 local dir=`pwd`
 local outdir=$1
 local hostname=$2
 local user=$3
 local port=$4
 local output_log=$5
 local appsetting=$6
 local src_root_dir=$7
 local commit_hash_file="$8"

 build_ASRS_package $outdir $hostname $appsetting $src_root_dir $commit_hash_file

 local status=$(check_build_status $outdir)
 if [ $status == 0 ]
 then
   launch_service $outdir $hostname $user $port $output_log
 fi
}

function stop_service() {
 local hostname=$1
 local user=$2
 local port=$3
 ssh -o StrictHostKeyChecking=no -p $port ${user}@${hostname} "killall ${g_service_runtime}"
}

function launch_service_on_single_vm() {
 local hostname=$1
 local user=$2
 local port=$3
 local outdir=$g_bin_folder_name
 local local_log=${hostname}.log
 launch_single_service $outdir $hostname $user $port $local_log
}

function launch_single_service() {
 local outdir=$1
 local hostname=$2
 local user=$3
 local port=$4
 local output_log=$5
 local auto_launch_script=auto_local_launch_service.sh

 ssh -o StrictHostKeyChecking=no -p $port ${user}@${hostname} "tar zxvf ${outdir}.tgz"
 local launch_service_pid_file="/tmp/launch_service.pid"
cat << EOF > $auto_launch_script
#!/bin/bash
#automatic generated script
cd ${outdir}
if [ -e $launch_service_pid_file ]
then
  pid=\`cat $launch_service_pid_file\`
  kill -9 \$pid
fi

killall ${g_service_runtime}
sleep 2 # wait for the exit of previous running

rm out.log
nohup ./${g_service_runtime} > out.log &
echo \$! > $launch_service_pid_file
EOF
 scp -o StrictHostKeyChecking=no -P ${port} $auto_launch_script ${user}@${hostname}:~
 ssh -o StrictHostKeyChecking=no -p ${port} ${user}@${hostname} "chmod +x $auto_launch_script"
 ssh -o StrictHostKeyChecking=no -p ${port} ${user}@${hostname} "./$auto_launch_script"

 local end=$((SECONDS + 60))
 local finish=0
 local check
 while [ $SECONDS -lt $end ] && [ "$finish" == "0" ]
 do
	scp -o StrictHostKeyChecking=no -P ${port} ${user}@${hostname}:~/${outdir}/out.log ${output_log}
	check=$(check_service_launch_status ${output_log})
	if [ $check -eq 0 ]
	then
		finish=1
		echo "service is started!"
                break
	else
		echo "wait for service starting..."
	fi
	sleep 1
 done
}

function stop_single_service() {
  local vm_host=$1
  local ssh_user=$2
  local ssh_port=$3
  ssh -o StrictHostKeyChecking=no -p ${ssh_port} ${ssh_user}@${vm_host} "killall ${g_service_runtime}"
}

function stop_service_on_all_vms() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port stop_single_service
}

function launch_service_on_all_vms() {
  local vm_list="$1"
  local user=$2
  local port=$3
  iterate_all_vms "$vm_list" $user $port launch_service_on_single_vm
}

function launch_service() {
 local outdir=$1
 local hostname=$2
 local user=$3
 local port=$4
 local output_log=$5
 local auto_launch_script=auto_local_launch_service.sh

 scp -o StrictHostKeyChecking=no -P $port ${outdir}.tgz ${user}@${hostname}:~/

 launch_single_service $outdir $hostname $user $port $output_log
}
