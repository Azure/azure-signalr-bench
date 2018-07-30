
g_CPU_requests="1|2|3|4"
g_CPU_limits="1|2|3|4"
g_Memory_limits="4000|4000|4000|4000"

function get_k8s_deploy_name() {
  local resName=$1
  local config_file=$2
  local len=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len -eq 0 ]
  then
    return
  fi
  local deployName=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.name'|tr -d '"'`
  echo $deployName
  #kubectl get deploy $deployName -o=json  --kubeconfig=$config_file
}

function update_k8s_deploy_replicas() {
  local deploy_name=$1
  local target_replicas=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/replicas', 'value': $target_replicas}]" --kubeconfig=$config_file
}

function update_k8s_deploy_env_connections() {
  local env_name
  local deploy_name=$1
  local connections_limit=$2
  local config_file=$3
  local env_len=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env|length'`
  if [ $env_len -eq 18 ]
  then
    env_name=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env[17].name'|tr -d '"'`
    if [ "$env_name" == "ConnectionCountLimit" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/env/17/value', 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi

    env_name=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env[16].name'|tr -d '"'`
    if [ "$env_name" == "MaxConcurrentUpgradedConnections" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/env/16/value', 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi

    env_name=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env[15].name'|tr -d '"'`
    if [ "$env_name" == "MaxConcurrentConnections" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/env/15/value', 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi
  fi
}

function get_pod() {
  local resName=$1
  local output=$2
  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi
  echo "$result"
  kubectl get deploy $result -o=json --kubeconfig=${config_file} > $output
}

function get_k8s_pod_status() {
  local resName=$1
  local outdir=$2
  local config_file=kvsignalrdevseasia.config
  local kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
  local len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len == "0" ]
  then
     config_file=srdevacsrpd.config
     kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
     if [ "$kubeId" == "" ]
     then
       echo "Cannot find $resName"
       return
     fi
     len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  fi
  local i=0
  while [ $i -lt $len ]
  do
     local podname=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq ".items[$i].metadata.name"|tr -d '"'`
     kubectl get pod $podname --kubeconfig=${config_file} > $outdir/${podname}_pod.txt
     kubectl get pod $podname -o=json --kubeconfig=${config_file} > $outdir/${podname}_pod.json
     i=`expr $i + 1`
  done
  
}

function k8s_get_pod_number() {
  local config_file=$1
  local resName=$2
  local kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
  local len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  echo $len
}

function k8s_query() {
  local config_file=$1
  local resName=$2
  local kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
  local len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len == "0" ]
  then
     return
  fi
  local i=0
  while [ $i -lt $len ]
  do
     kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq ".items[$i].metadata.name"|tr -d '"'
     i=`expr $i + 1`
  done
}

function update_k8s_deploy_cpu_limits() {
  local deploy_name=$1
  local cpu_limit=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/resources/limits/cpu', 'value': '$cpu_limit'}]" --kubeconfig=$config_file
}

function update_k8s_deploy_cpu_request() {
  local deploy_name=$1
  local cpu_limit=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/resources/requests/cpu', 'value': '$cpu_limit'}]" --kubeconfig=$config_file
}

function update_k8s_deploy_memory_limits() {
  local deploy_name=$1
  local memory_limit=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/resources/limits/memory', 'value': '$memory_limit'}]" --kubeconfig=$config_file
}

function update_k8s_deploy_liveprobe_timeout() {
  local deploy_name=$1
  local timeout=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/template/spec/containers/0/livenessProbe/timeoutSeconds', 'value': $timeout}]" --kubeconfig=$config_file
}

function install_nettools() {
  local pod_name=$1
  local config_file=$2
  kubectl exec --kubeconfig=${config_file} ${pod_name} apt-get install net-tools
}

function start_connection_tracking() {
  local i
  local resName=$1
  local output_dir=$2
  local config_file=kvsignalrdevseasia.config
  local result=$(k8s_query $config_file $resName)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(k8s_query $config_file $resName)
  fi
  echo "'$result'"
  # install netstat
  for i in $result
  do
     kubectl exec --kubeconfig=$config_file $i apt-get install net-tools > /dev/null
  done
  # collect connections
  while [ 1 ]
  do
     for i in $result
     do
       local date_time=`date --iso-8601='seconds'`
       local cli_connection=`kubectl exec $i --kubeconfig=$config_file -- bash -c "netstat -an|grep 5001|grep EST|wc -l"`
       local ser_connection=`kubectl exec $i --kubeconfig=$config_file -- bash -c "netstat -an|grep 5002|grep EST|wc -l"`
       echo "${date_time} $cli_connection $ser_connection" >> $output_dir/${i}_connections.txt
     done
     sleep 1
  done
}

function stop_connection_tracking() {
  local connection_start_pid=$1
  kill $connection_start_pid
}

function copy_syslog() {
  local i
  local resName=$1
  local outdir=$2
  local config_file=kvsignalrdevseasia.config
  local result=$(k8s_query $config_file $resName)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(k8s_query $config_file $resName)
  fi
  for i in $result
  do
     kubectl cp default/${i}:/var/log/syslog $outdir/${i}_syslog.txt --kubeconfig=$config_file
     if [ -e $outdir/${i}_syslog.txt ]
     then
        cd $outdir
        tar zcvf ${i}_syslog.tgz ${i}_syslog.txt
        rm ${i}_syslog.txt
        cd -
     fi
  done

}

function wait_replica_ready() {
  local config_file=$1
  local resName=$2
  local replica=$3
  local pods
  local end=$((SECONDS + 120))
  while [ $SECONDS -lt $end ]
  do
    pods=$(k8s_get_pod_number $config_file $resName)
    if [ $pods -eq $replica ]
    then
      break
    fi
    sleep 1
  done
}

function wait_deploy_ready() {
  local deploy=$1
  local config_file=$2
  local end=$((SECONDS + 120))
  while [ $SECONDS -lt $end ]
  do
    echo kubectl rollout status deployment/$result --kubeconfig=$config_file
    kubectl rollout status deployment/$result --kubeconfig=$config_file
    if [ $? -eq 0 ]
    then
      break
    fi
    sleep 1
  done
}

function patch_liveprobe_timeout() {
  local resName=$1
  local timeout=$2 # second
  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi

  local pods=$(k8s_get_pod_number $config_file $resName)
  update_k8s_deploy_liveprobe_timeout $result "$timeout" $config_file
  wait_deploy_ready $result $config_file

  wait_replica_ready $config_file $resName $pods
}

function patch_connection_throttling_env() {
  local resName=$1
  local connection_limit=$2
  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi

  local pods=$(k8s_get_pod_number $config_file $resName)
  update_k8s_deploy_env_connections $result "${connection_limit}" $config_file

  wait_deploy_ready $result $config_file

  wait_replica_ready $config_file $resName $pods
}

function patch_replicas_env() {
  local resName=$1
  local replicas=$2
  local connection_limit=$3

  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi
  #echo "$result"
  update_k8s_deploy_replicas $result $replicas $config_file
  update_k8s_deploy_env_connections $result "${connection_limit}" $config_file

  wait_deploy_ready $result $config_file
}

function patch_replicas() {
  local resName=$1
  local replicas=$2
  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi
  #echo "$result"
  update_k8s_deploy_replicas $result $replicas $config_file

  wait_deploy_ready $result $config_file
}

# resource_name, replicas, cpu_limit, cpu_req, mem_limit, connect_limit
function patch() {
  local resName=$1
  local replicas=$2
  local cpu_limit=$3
  local cpu_req=$4
  local mem_limit=$5
  local connect_limit=$6
  local config_file=kvsignalrdevseasia.config
  local result=$(get_k8s_deploy_name $resName $config_file)
  if [ "$result" == "" ]
  then
     config_file=srdevacsrpd.config
     result=$(get_k8s_deploy_name $resName $config_file)
  fi
  #echo "$result"
  update_k8s_deploy_replicas $result $replicas $config_file

  update_k8s_deploy_cpu_limits $result "$cpu_limit" $config_file

  update_k8s_deploy_cpu_request $result "$cpu_req" $config_file

  update_k8s_deploy_memory_limits $result "${mem_limit}Mi" $config_file

  update_k8s_deploy_env_connections $result "${connect_limit}" $config_file

  wait_deploy_ready $result $config_file

  wait_replica_ready $config_file $resName $replicas
}

function patch_and_wait() {
  local name=$1
  local rsg=$2
  local index=$3
  local replica=$4
  local cpu_req=$(array_get $g_CPU_requests $index "|")
  local cpu_limit=$(array_get $g_CPU_limits $index "|")
  local mem_limit=$(array_get $g_Memory_limits $index "|")
  patch ${name} $replica $cpu_limit $cpu_req $mem_limit 500000
  patch_liveprobe_timeout ${name} 2
}
