#!/bin/bash
. ./func_env.sh
# input Jenkinw workspace directory
# this function is invoked in every job entry
function set_global_env() {
   local Jenkins_Workspace_Root=$1
   export JenkinsRootPath="$Jenkins_Workspace_Root"
   export ScriptWorkingDir=$Jenkins_Workspace_Root/azure-signalr-bench/v1                     # folders to find all scripts
   export CurrentWorkingDir=$Jenkins_Workspace_Root/azure-signalr-bench/v2/JenkinsScript/     # workding directory

############# those configurations are shared in Jenkins folder #####
   export AgentConfig=$JenkinsRootPath'/agent.yaml'
   export PrivateIps=$JenkinsRootPath'/privateIps.yaml'
   export PublicIps=$JenkinsRootPath'/publicIps.yaml'
   export ServicePrincipal=$JenkinsRootPath'/servicePrincipal.yaml'

############ global static variables #########
   export RootFolder="$CurrentWorkingDir"
   export ConfigRoot="$RootFolder/signalr_perf_config_sample/config/"
   export ScenarioRoot="$RootFolder/signalr_perf_config_sample/scenarios/"
   export BenchConfig=$ConfigRoot'/bench.yaml'
   #export ResultFolderSuffix='suffix'
   export SendToFixedClient='true'
   export VMMgrDir=/tmp/VMMgr
}

# depends on set_global_env
function set_job_env() {
   export result_root=`date +%Y%m%d%H%M%S`
   export DogFoodResourceGroup="honzhanatpf"`date +%M%S`
   export serverUrl=`awk '{print $2}' $JenkinsRootPath/JobConfig.yaml`
}

# depends on global env:
#  RootFolder, Branch, PrivateIps, PublicIps, AgentConfig, JobConfig
#  SendToFixedClient, ServicePrincipal, env_statistic_folder,
#  ScriptWorkingDir
function run_and_gen_report()
{
   local connectStr=$1
   local tag=$2
   local Scenario=$3
   local Transport=$4
   local MessageEncoding=$5
   local connection=$6
   local concurrentConnection=$7
   local send=$8
   local ConnectionString=$9
   ############## run bench #####################
   cd $RootFolder
   local connectionStringOpt=""
   ## handle "RestSendToUser" and "RestBroadcast"
   if [[ "$Scenario" == "Rest"* ]]
   then
      connectionStringOpt="--connectionString='$ConnectionString'"
   fi
   dotnet run -- --PidFile='./pid/pid_'$result_root'.txt' --step=AllInSameVnet \
    --branch=$Branch \
    --PrivateIps=$PrivateIps \
    --PublicIps=$PublicIps \
    --AgentConfigFile=$AgentConfig \
    --JobConfigFileV2=$JobConfig \
    --sendToFixedClient=$SendToFixedClient \
    --stopSendIfLatencyBig="true" \
    --stopSendIfConnectionErrorBig="true" \
    --ServicePrincipal=$ServicePrincipal \
    --AzureSignalrConnectionString=$connectStr "$connectionStringOpt"
   ############# gen report ##############
   local counterPath=`find ${env_statistic_folder} -iname "counters.txt"`
   if [ "$counterPath" != "" ]
   then
     cp $counterPath ${env_statistic_folder}
     cd $ScriptWorkingDir
     #### generate the connection configuration for HTML ####
cat << EOF > configs/cmd_4_${MessageEncoding}_${Scenario}_${tag}_${Transport}
connection=${connection}
connection_concurrent=${concurrentConnection}
send=${send}
bench_config_endpoint="$serverUrl"
EOF

     ## gen_html.sh requires bench_name_list, bench_codec_list, and bench_type_list
     export bench_name_list="$Scenario"
     export bench_codec_list="$MessageEncoding"
     export bench_type_list="${tag}_${Transport}"
     sh gen_html.sh $ConnectionString
   fi
}

function gen_sendtoclient_job_config()
{
    local tag=$1
    local Transport=$2
    local MessageEncoding=$3
    local Scenario=$4
    local unit=$5
    local msgSize=$6
    local maxConnectionOption=""
    if [ "$useMaxConnection" == "true" ]
    then
       maxConnectionOption="-M"
    fi
    send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 0 -S --sendToClientSz $msgSize ${maxConnectionOption}`
cat << EOF > $JobConfig
serviceType: $tag
transportType: ${Transport}
hubProtocol: ${MessageEncoding}
scenario: ${Scenario}
EOF
    python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} --sendToClientSz $msgSize -d ${sigbench_run_duration} ${maxConnectionOption}>>$JobConfig
    cat << EOF >> $JobConfig
serverUrl: ${serverUrl}
messageSize: ${msgSize}
EOF
}

#####################################################
# depends on global env:
# useMaxConnection
function gen_sendgroup_job_config()
{
    local tag=$1
    local Transport=$2
    local MessageEncoding=$3
    local Scenario=$4
    local unit=$5
    local j=$6
    local messageSize=$7
    local maxConnectionOption=""
    if [ "$useMaxConnection" == "true" ]
    then
       maxConnectionOption="-M"
    fi
    local groupOption=""
    cd $ScriptWorkingDir
    if [ "$j" == "smallGroup" ]
    then
      groupOption="-g"
    else if [ "$j" == "bigGroup" ]
         then
           groupOption="-G"
         else if [ "$j" == "tinyGroup" ]
              then
                 groupOption="-y"
              else if [ "$j" == "overlapGroup" ]
                   then
                      groupOption="-p"
                   fi
              fi
         fi
    fi
    send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 0 -S $groupOption ${maxConnectionOption}`
cat << EOF > $JobConfig
serviceType: $tag
transportType: ${Transport}
hubProtocol: ${MessageEncoding}
scenario: ${Scenario}
EOF
    python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} $groupOption -d ${sigbench_run_duration} ${maxConnectionOption}>>$JobConfig
    cat << EOF >> $JobConfig
serverUrl: ${serverUrl}
messageSize: ${messageSize}
EOF
}

function gen_job_config()
{
     local tag=$1
     local Transport=$2
     local MessageEncoding=$3
     local Scenario=$4
     local unit=$5
     local messageSize=$6
     local maxConnectionOption=""
     if [ "$useMaxConnection" == "true" ]
     then
        maxConnectionOption="-M"
     fi
     cat << EOF > $JobConfig
serviceType: $tag
transportType: ${Transport}
hubProtocol: ${MessageEncoding}
scenario: ${Scenario}
EOF
     cd $ScriptWorkingDir
     python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d ${sigbench_run_duration} ${maxConnectionOption}>>$JobConfig
     cat << EOF >> $JobConfig
serverUrl: ${serverUrl}
messageSize: ${messageSize}
EOF
}

function prepare_result_folder_4_scenario()
{
     local tag=$1
     local Transport=$2
     local MessageEncoding=$3
     local Scenario=$4
     ## take care of the gen of this folder which is also required by gen_html.sh
     local scenario_folder=${tag}_${Transport}_${MessageEncoding}_${Scenario}
     # do not forget the tailing '/'
     export env_statistic_folder="${ScriptWorkingDir}/${result_root}/${scenario_folder}/"
     export env_result_folder=$env_statistic_folder                                         # tell the dotnet program where to save counters.txt
     mkdir -p ${env_statistic_folder}
}

###
## depends on global env:
##   customerList, serverUrl
function run_customer_bench()
{
   local unit=$1
   local connection
   local send
   local concurrentConnection
   local tag="unit"$unit
   local Scenario
   local Transport
   local MessageEncoding
   local connectStr
   local i=0 j k
   while [ $i -lt $serverVmCount ]
   do
     if [ $i -eq 0 ]
     then
       connectStr=${ConnectionString}
     else
       connectStr="${connectStr}^${ConnectionString}"
     fi
     i=$(($i + 1))
   done

   for k in $customerList
   do
     Transport=`python query_customer.py -c $k -i Transport`
     Scenario=`python query_customer.py -c $k -i Scenario`
     MessageEncoding=`python query_customer.py -c $k -i Protocol`
     send=`python query_customer.py -c $k -i Send`
     connection=`python query_customer.py -c $k -i Connection`
     concurrentConnection=`python query_customer.py -c $k -i ConcurrentConnection`
     bench_send_size=`python query_customer.py -c $k -i MessageSize`
     if [ ! -d $ScenarioRoot"/${Scenario}" ]
     then
        mkdir $ScenarioRoot"/${Scenario}"
     fi
     export JobConfig=$ScenarioRoot"/${Scenario}/job.yaml"
     cd $ScriptWorkingDir
     tag=${tag}_${k}
     prepare_result_folder_4_scenario ${tag} ${Transport} ${MessageEncoding} ${Scenario}
     ############## configure scenario ############
     send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 0 -S`
     python query_customer.py -c $k -i JobConfig > $JobConfig
     cat << EOF >> $JobConfig
serverUrl: ${serverUrl}
EOF
#gen_job_config $tag ${Transport} ${MessageEncoding} ${Scenario} ${unit} ${bench_send_size}
     ############## run bench #####################
     run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString  
   done
}

#####################################################
## This step run benchmark per different scenarios ##
#####################################################
# depends on global env:
#   serverVmCount, bench_scenario_list, bench_scenario_list,
#   bench_encoding_list, ScenarioRoot, ScriptWorkingDir, GroupTypeList,
#   bench_send_size, serverUrl
function run_benchmark()
{
   local unit=$1
   local connection
   local send
   local concurrentConnection
   local tag="unit"$unit
   local Scenario
   local Transport
   local MessageEncoding
   local connectStr
   local i=0 j
   while [ $i -lt $serverVmCount ]
   do
     if [ $i -eq 0 ]
     then
       connectStr=${ConnectionString}
     else
       connectStr="${connectStr}^${ConnectionString}"
     fi
     i=$(($i + 1))
   done
   
   for Scenario in $bench_scenario_list
   do
       for Transport in $bench_transport_list
       do
          for MessageEncoding in $bench_encoding_list
          do
             if [ ! -d $ScenarioRoot"/${Scenario}" ]
             then
                mkdir $ScenarioRoot"/${Scenario}"
             fi
             export JobConfig=$ScenarioRoot"/${Scenario}/job.yaml"
             cd $ScriptWorkingDir
             local maxConnectionOption=""
             if [ "$useMaxConnection" == "true" ]
             then
                maxConnectionOption="-M"
             fi
             connection=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -c ${maxConnectionOption}`
             concurrentConnection=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -C ${maxConnectionOption}`
             tag="unit"${unit}
             ## special handle SendGroup
             if [ "$Scenario" == "SendGroup" ]
             then
               for j in $GroupTypeList
               do
                 tag="unit"${unit}"_${j}"
                 prepare_result_folder_4_scenario ${tag} ${Transport} ${MessageEncoding} ${Scenario}
             ############## configure scenario ############
                 gen_sendgroup_job_config ${tag} ${Transport} ${MessageEncoding} ${Scenario} ${unit} ${j} ${bench_send_size}
                 ############## run bench #####################
                 run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString
               done
             else if [ "$Scenario" == "sendToClient" ]
                  then
                    for j in $sendToClientMsgSize
                    do
                      tag="unit"${unit}"_${j}"
                      prepare_result_folder_4_scenario ${tag} ${Transport} ${MessageEncoding} ${Scenario}
                      ############## configure scenario ############
                      send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 0 --sendToClientSz $j -S ${maxConnectionOption}`
                      gen_sendtoclient_job_config $tag ${Transport} ${MessageEncoding} ${Scenario} ${unit} ${j}
                      ############## run bench #####################
                      run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString
                    done
                  else
                    prepare_result_folder_4_scenario ${tag} ${Transport} ${MessageEncoding} ${Scenario}
                    ############## configure scenario ############
                    send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 0 -S ${maxConnectionOption}`
                    gen_job_config $tag ${Transport} ${MessageEncoding} ${Scenario} ${unit} ${bench_send_size}
                    ############## run bench #####################
                    run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString
                  fi
             fi
             ## restart the pod to fresh run next scenario

          done  
       done
   done
}

function create_asrs()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4

. ./az_signalr_service.sh
. ./kubectl_utils.sh  

  local signalr_service=$(create_signalr_service $rsg $name $sku $unit)
  if [ "$signalr_service" == "" ]
  then
    echo "Fail to create SignalR Service"
    return
  else
    echo "Create SignalR Service '${signalr_service}'"
  fi
  local dns_ready=$(check_signalr_service_dns $rsg $name)
  if [ $dns_ready -eq 1 ]
  then
    echo "SignalR Service DNS is not ready, suppose it is failed!"
    delete_signalr_service $name $rsg
    return
  fi
  if [ "$ASRSEnv" == "dogfood" ]
  then
    if [ "$Disable_UNIT_PER_POD" == "true" ]
    then
      patch_deployment_for_no_tc_and_wait $name 100
    fi
    if [ "$Disable_Connection_Throttling" == "true" ]
    then
      local limit=`python gen_connection_throttling.py -u "unit"$unit`
      if [ $limit -ne 0 ]
      then
         patch_connection_throttling_env $name $limit
      fi
    fi
  fi
  local connectionStr=$(query_connection_string $name $rsg)
  echo "[ConnectionString]: $connectionStr"
  export ConnectionString="$connectionStr"
}

function override_default_setting() {
cd $ScriptWorkingDir
cat << EOF >jenkins_env.sh
nginx_root=$nginx_root
sigbench_run_duration=$sigbench_run_duration
EOF
}

## run perf test against existing connection string, no creation of ASRS.
# the bench_serviceunit_list should contain only one element
#
# global env:
# bench_serviceunit_list, ScriptWorkingDir, result_root
# copy_syslog, copy_nginx_log
# specify connection string as input parameter
function run_unit() {
   local ConnectionString="$1"
   local callback=$2
   local service
   local signalrServiceName
for service in $bench_serviceunit_list
do
   cd $ScriptWorkingDir
   if [ "$ConnectionString" == "" ]
   then
     echo "Skip the running on SignalR service unit'$service' since it was failed to create"
     continue
   fi

   cd $ScriptWorkingDir
   . ./func_env.sh
   . ./kubectl_utils.sh

   ## service folder to save
   local k8s_result_dir=${ScriptWorkingDir}/${result_root}/"unit"$service
   mkdir -p $k8s_result_dir

   ## start collecting top information
   local service_name=$(extract_servicename_from_connectionstring $ConnectionString)
   if [ "$service_name" != "" ]
   then
     nohup sh collect_pod_top.sh $service_name $k8s_result_dir &
     collect_pod_top_pid=$!
     if [ "$g_nginx_ns" != "" ]
     then
        nohup sh collect_nginx_top.sh $service_name $g_nginx_ns $k8s_result_dir &
        collect_nginx_top_pid=$!
     fi
   fi
   #############
   $callback $service
   ############# copy pod log ############
   ## stop collecting top
   if [ "$service_name" != "" ]
   then
      kill $collect_pod_top_pid
      if [ "$collect_nginx_top_pid" != "" ]
      then
         kill $collect_nginx_top_pid
      fi
   ############# copy pod log ############
      if [ "$copy_syslog" == "true" ]
      then
         copy_syslog $service_name $k8s_result_dir
      fi
      if [ "$copy_nginx_log" == "true" ]
      then
         get_nginx_log $service_name "$g_nginx_ns" $k8s_result_dir
      fi
      get_k8s_pod_status $service_name $k8s_result_dir
   fi
done

}

# global env:
# bench_serviceunit_list, ScriptWorkingDir, result_root
# copy_syslog, copy_nginx_log
function run_all_units() {
local service
local signalrServiceName
for service in $bench_serviceunit_list
do
   cd $ScriptWorkingDir
   ConnectionString="" # set it to be invalid first
   signalrServiceName="atpf"`date +%H%M%S`
   create_asrs $DogFoodResourceGroup $signalrServiceName $Sku $service
   if [ "$ConnectionString" == "" ]
   then
     echo "Skip the running on SignalR service unit'$service' since it was failed to create"
     continue
   fi

   cd $ScriptWorkingDir
   . ./func_env.sh
   . ./kubectl_utils.sh

   ## service folder to save
   local k8s_result_dir=${ScriptWorkingDir}/${result_root}/"unit"$service
   mkdir -p $k8s_result_dir
   
   ## start collecting top information
   local service_name=$(extract_servicename_from_connectionstring $ConnectionString)
   if [ "$service_name" != "" ]
   then
     nohup sh collect_pod_top.sh $service_name $k8s_result_dir &
     collect_pod_top_pid=$!
     if [ "$g_nginx_ns" != "" ]
     then
        nohup sh collect_nginx_top.sh $service_name $g_nginx_ns $k8s_result_dir &
        collect_nginx_top_pid=$!
     fi
   fi
   #############
   run_benchmark $service
   ############# copy pod log ############
   ## stop collecting top
   if [ "$service_name" != "" ]
   then
      if [ "$collect_pod_top_pid" != "" ]
      then
         # kill the process if it is alive
         local a=`ps -o pid= -p $collect_pod_top_pid`
         if [ "$a" != "" ]
         then
            kill $collect_pod_top_pid
         fi
      fi
      if [ "$collect_nginx_top_pid" != "" ]
      then
         local a=`ps -o pid= -p $collect_nginx_top_pid`
         if [ "$a" != "" ]
         then
            kill $collect_nginx_top_pid
         fi
      fi
   ############# copy pod log ############
      if [ "$copy_syslog" == "true" ]
      then
         copy_syslog $service_name $k8s_result_dir
      fi
      if [ "$copy_nginx_log" == "true" ]
      then
         get_nginx_log $service_name "$g_nginx_ns" $k8s_result_dir
      fi
      get_k8s_pod_status $service_name $k8s_result_dir
   fi
   ######
   
   delete_signalr_service $signalrServiceName $DogFoodResourceGroup
done

}
# require global env:
# ASRSEnv, DogFoodResourceGroup, ASRSLocation
function prepare_ASRS_creation() {
cd $ScriptWorkingDir
. ./az_signalr_service.sh

if [ "$ASRSEnv" == "dogfood" ]
then
  register_signalr_service_dogfood
  az_login_ASRS_dogfood
else
  az_login_signalr_dev_sub
fi
create_group_if_not_exist $DogFoodResourceGroup $ASRSLocation
}

# global env: ScriptWorkingDir, DogFoodResourceGroup, ASRSEnv
function clean_ASRS_group() {
############# remove SignalR Service Resource Group #########
cd $ScriptWorkingDir
delete_group $DogFoodResourceGroup
if [ "$ASRSEnv" == "dogfood" ]
then
  unregister_signalr_service_dogfood
fi
}

## exit handler to remove resource group ##
# global env:
# CurrentWorkingDir, ServicePrincipal, AgentConfig, VMMgrDir
function remove_resource_group() {
  echo "!!Received EXIT!! and remove all created VMs"

  cd $CurrentWorkingDir
  local clean_vm_daemon=daemon_${JOB_NAME}_cleanvms
  local clean_asrs_daemon=daemon_${JOB_NAME}_cleanasrs
  ## remove all test VMs
  daemonize -o /tmp/${clean_vm_daemon}.out -e /tmp/${clean_vm_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup ${VMMgrDir}/JenkinsScript --PidFile='./pid/pid_remove_rsg.txt' --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal &

  ## remove ASRS
cat << EOF > /tmp/clean_asrs.sh
cd $ScriptWorkingDir
. ./az_signalr_service.sh

if [ "$ASRSEnv" == "dogfood" ]
then
  az_login_ASRS_dogfood
  delete_group $DogFoodResourceGroup
  unregister_signalr_service_dogfood
else
  az_login_signalr_dev_sub
  delete_group $DogFoodResourceGroup
fi
EOF
  daemonize -o /tmp/${clean_asrs_daemon}.out -e /tmp/${clean_asrs_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh /tmp/clean_asrs.sh &
}

## register exit handler to remove resource group ##
# global env:
# CurrentWorkingDir VMMgrDir
function register_exit_handler() {
  cd $CurrentWorkingDir
  dotnet publish -c Release -f netcoreapp2.1 -o ${VMMgrDir}  --self-contained -r linux-x64
  trap remove_resource_group EXIT
}
