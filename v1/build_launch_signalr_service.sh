#!/bin/bash
g_service_runtime=Microsoft.Azure.SignalR.ServiceRuntime

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

function launch_service() {
 local outdir=$1
 local hostname=$2
 local user=$3
 local port=$4
 local output_log=$5
 local auto_launch_script=auto_local_launch_service.sh

 scp -o StrictHostKeyChecking=no -P $port ${outdir}.tgz ${user}@${hostname}:~/

 ssh -o StrictHostKeyChecking=no -p $port ${user}@${hostname} "tar zxvf ${outdir}.tgz"

cat << EOF > $auto_launch_script
#!/bin/bash
#automatic generated script
ssh -o StrictHostKeyChecking=no -p $port ${user}@${hostname} "killall -9 ${g_service_runtime}"
sleep 2 # wait for the exit of previous running
ssh -o StrictHostKeyChecking=no -p $port ${user}@${hostname} "cd $outdir; ./${g_service_runtime}"
EOF

 nohup sh $auto_launch_script > ${output_log} 2>&1 &

 local end=$((SECONDS + 60))
 local finish=0
 local check
 while [ $SECONDS -lt $end ] && [ "$finish" == "0" ]
 do
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
