
g_CPU_requests="1|2|3|4"
g_CPU_limits="1|2|3|4"
g_Memory_limits="4000|4000|4000|4000"
g_k8s_config_list="srdevacsrpe.json|kubeconfig_srdevacsseasiac.json"

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

function find_target_by_iterate_all_k8slist()
{
  local resName=$1
  local callback=$2
  local config
  local result
  local i=1
  local ns=""
  if [ $# -eq 3 ]
  then
    ns=$3
  fi
  local len=$(array_len "$g_k8s_config_list" "|")
  while [ $i -le $len ]
  do
     config=$(array_get $g_k8s_config_list $i "|")
     result=$($callback $resName $config "$ns")
     if [ "$result" != "" ]
     then
        g_config=$config
        g_result=$result
        break
     fi
     i=$(($i + 1))
  done
}

function get_k8s_deploy_name() {
  local resName=$1
  local config_file=$2
  local ns="default"
  if [ $# -eq 3 ]
  then
    ns=$3
  fi
  local len=`kubectl get deploy -o=json --selector resourceName=$resName --namespace=${ns} --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len -eq 0 ]
  then
    return
  fi
  local deployName=`kubectl get deploy -o=json --selector resourceName=$resName --namespace=${ns} --kubeconfig=${config_file}|jq '.items[0].metadata.name'|tr -d '"'`
  echo $deployName
  #kubectl get deploy $deployName -o=json  --kubeconfig=$config_file
}

function update_k8s_deploy_replicas() {
  local deploy_name=$1
  local target_replicas=$2
  local config_file=$3
  kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': '/spec/replicas', 'value': $target_replicas}]" --kubeconfig=$config_file
}

function read_k8s_deploy_env() {
  local env_name
  local deploy_name=$1
  local connections_limit=$2
  local config_file=$3
  local i=0
  local env_len=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env|length'`
  while [ $i -lt $env_len ]
  do
    env_name=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq ".spec.template.spec.containers[0].env[$i].name"|tr -d '"'`
    echo $env_name
    i=$(($i+1))
  done
}

function update_k8s_deploy_env_connections() {
  local env_name
  local deploy_name=$1
  local connections_limit=$2
  local config_file=$3
  local i=0
  local env_len=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq '.spec.template.spec.containers[0].env|length'`
  while [ $i -lt $env_len ]
  do
    env_name=`kubectl get deployment $deploy_name -o=json --kubeconfig=${config_file}|jq ".spec.template.spec.containers[0].env[$i].name"|tr -d '"'`
    if [ "$env_name" == "ConnectionCountLimit" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': "/spec/template/spec/containers/0/env/$i/value", 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi

    if [ "$env_name" == "MaxConcurrentUpgradedConnections" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': "/spec/template/spec/containers/0/env/$i/value", 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi

    if [ "$env_name" == "MaxConcurrentConnections" ]
    then
      kubectl patch deployment $deploy_name --type=json -p="[{'op': 'replace', 'path': "/spec/template/spec/containers/0/env/$i/value", 'value': '$connections_limit'}]" --kubeconfig=$config_file
    fi
    i=$(($i+1))
  done
}

function get_pod() {
  local resName=$1
  local output=$2
  local ns="default"
  if [ $# -eq 3 ]
  then
     ns="$3"
  fi
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name "$ns"
  local config_file=$g_config
  local result=$g_result
  echo "$result"
  kubectl get deploy $result -o=json --kubeconfig=${config_file} > $output
}

function get_kube_deployment()
{
  local resName=$1
  local config_file=$2
  local kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
  if [ "$kubeId" == "" ]
  then
    echo ""
    return
  fi
  local len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len != "0" ]
  then
    echo "$kubeId"
  else
    echo ""
  fi
}

function get_k8s_pod_status() {
  local resName=$1
  local outdir=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_kube_deployment
  local config_file=$g_config
  local kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
  if [ "$kubeId" == "" ]
  then
    echo "Cannot find $resName"
    return
  fi

  local len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  if [ $len == "0" ]
  then
     echo "Cannot find $resName"
     return
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
  echo "$len"
}

function k8s_query() {
  local config_file=$2
  local resName=$1
  local kubeId len
  local ns=""
  if [ $# -eq 3 ]
  then
    ns=$3
    kubeId=`kubectl get deploy -o=json --namespace=$ns --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
    len=`kubectl get pod -o=json --namespace=$ns --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  else
    kubeId=`kubectl get deploy -o=json --selector resourceName=$resName --kubeconfig=${config_file}|jq '.items[0].metadata.labels.resourceKubeId'|tr -d '"'`
    len=`kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq '.items|length'`
  fi
  if [ "$len" == "0" ]
  then
     return
  fi
  local i=0
  while [ $i -lt $len ]
  do
     if [ "$ns" == "" ]
     then
       kubectl get pod -o=json --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq ".items[$i].metadata.name"|tr -d '"'
     else
       kubectl get pod -o=json --namespace=$ns --selector resourceKubeId=$kubeId --kubeconfig=${config_file}|jq ".items[$i].metadata.name"|tr -d '"'
     fi
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

function start_top_tracking() {
  local i
  local resName=$1
  local output_dir=$2
  local ns=""
  g_config=""
  g_result=""
  if [ $# -eq 3 ]
  then
    ns=$3
  fi
  find_target_by_iterate_all_k8slist $resName k8s_query $ns
  local config_file=$g_config
  local result=$g_result
  echo "'$result'"
  while [ 1 ]
  do
     for i in $result
     do
       local date_time=`date --iso-8601='seconds'`
       echo "${date_time} " >> $output_dir/${i}_top.txt
       kubectl exec $i --kubeconfig=$config_file -- bash -c "top -b -n 1" >> $output_dir/${i}_top.txt
     done
     sleep 1
  done
}

function stop_top_tracking() {
  local top_start_pid=$1
  kill $top_start_pid
}

function start_connection_tracking() {
  local i
  local resName=$1
  local output_dir=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName k8s_query
  local config_file=$g_config
  local result=$g_result
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
       local cli_ser_stat=`kubectl exec $i --kubeconfig=$config_file -- bash -c "curl http://localhost:5003/health/stat" 2> /dev/null`
       echo "${date_time} ${cli_ser_stat}" >> $output_dir/${i}_connections.txt
       #local cli_connection=`kubectl exec $i --kubeconfig=$config_file -- bash -c "netstat -an|grep 5001|grep EST|wc -l"`
       #local ser_connection=`kubectl exec $i --kubeconfig=$config_file -- bash -c "netstat -an|grep 5002|grep EST|wc -l"`
       #echo "${date_time} $cli_connection $ser_connection" >> $output_dir/${i}_connections.txt
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
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName k8s_query
  local config_file=$g_config
  local result=$g_result

  for i in $result
  do
     kubectl cp default/${i}:/var/log/ASRS/ASRS.log $outdir/${i}_ASRS.txt --kubeconfig=$config_file
     if [ -e $outdir/${i}_ASRS.txt ]
     then
        cd $outdir
        tar zcvf ${i}_ASRS.tgz ${i}_ASRS.txt
        rm ${i}_ASRS.txt
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
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result

  local pods=$(k8s_get_pod_number $config_file $resName)
  update_k8s_deploy_liveprobe_timeout $result "$timeout" $config_file
  wait_deploy_ready $result $config_file

  wait_replica_ready $config_file $resName $pods
}

function patch_connection_throttling_env() {
  local resName=$1
  local connection_limit=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result

  local pods=$(k8s_get_pod_number $config_file $resName)
  update_k8s_deploy_env_connections $result "${connection_limit}" $config_file

  wait_deploy_ready $result $config_file

  wait_replica_ready $config_file $resName $pods
}

function read_connection_throttling_env() {
  local resName=$1
  local connection_limit=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result

  local pods=$(k8s_get_pod_number $config_file $resName)
  read_k8s_deploy_env $result "${connection_limit}" $config_file
}

function patch_replicas_env() {
  local resName=$1
  local replicas=$2
  local connection_limit=$3

  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result
  #echo "$result"
  update_k8s_deploy_replicas $result $replicas $config_file
  update_k8s_deploy_env_connections $result "${connection_limit}" $config_file

  wait_deploy_ready $result $config_file
}

function patch_replicas() {
  local resName=$1
  local replicas=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result

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
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $resName get_k8s_deploy_name
  local config_file=$g_config
  local result=$g_result

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
  #patch_liveprobe_timeout ${name} 2
}

function get_nginx_pod_internal() {
  local res=$1
  local config=$2
  local ns=$3
  local appId=`kubectl get deploy -o=json --namespace=${ns} --selector resourceName=${res} --kubeconfig=${config}|jq '.items[0].spec.selector.matchLabels.app'|tr -d '"'`
  local len=`kubectl get pod -o=json --namespace=${ns} --selector app=${appId} --kubeconfig=${config}|jq '.items|length'`
  local i=0
  while [ $i -lt $len ]
  do
    kubectl get pod -o=json --namespace=$ns --selector app=${appId} --kubeconfig=${config}|jq ".items[$i].metadata.name"|tr -d '"'
    i=`expr $i + 1`
  done
}

function get_nginx_pod() {
  local res=$1
  local ns=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $res get_nginx_pod_internal "$ns"
  echo "$g_result"
}

function track_nginx_top() {
  local res=$1
  local ns=$2
  local output_dir=$3
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $res get_nginx_pod_internal $ns
  local config_file=$g_config
  local result=$g_result

  while [ 1 ]
  do
     for i in $result
     do
       local date_time=`date --iso-8601='seconds'`
       echo "${date_time} " >> $output_dir/${i}_top.txt
       kubectl exec $i --namespace=$ns --kubeconfig=$config_file -- bash -c "top -b -n 1" >> $output_dir/${i}_top.txt
     done
     sleep 1
  done
}

function get_nginx_log() {
  local res=$1
  local ns=$2
  local outdir=$3
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $res get_nginx_pod_internal $ns
  local config_file=$g_config
  local result=$g_result
  for i in $result
  do
    kubectl logs $i --namespace=$ns --kubeconfig=$config_file > $outdir/${i}.log
  done
}

function delete_all_nginx_pods() {
  local res=$1
  local ns=$2
  g_config=""
  g_result=""
  find_target_by_iterate_all_k8slist $res get_nginx_pod_internal $ns
  local config_file=$g_config
  local result=$g_result
  for i in $result
  do
    kubectl delete pods $i --namespace=$ns --kubeconfig=$config_file
  done

}
