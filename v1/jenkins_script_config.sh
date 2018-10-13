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
   export DogFoodResouceGroup="honzhanatpf"`date +%M%S`
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

#####################################################
## This step run benchmark per different scenarios ##
#####################################################
# depends on global env:
#   serverVmCount, bench_scenario_list, bench_scenario_list,
#   bench_encoding_list, ScenarioRoot, ScriptWorkingDir, GroupTypeList,
#   bench_send_size
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
                 local scenario_folder=${tag}_${Transport}_${MessageEncoding}_${Scenario}   ## take care of the gen of this folder which is also required by gen_html.sh
                 # do not forget the tailing '/'
                 export env_statistic_folder="${ScriptWorkingDir}/${result_root}/${scenario_folder}/"
                 export env_result_folder=$env_statistic_folder                                         # tell the dotnet program where to save counters.txt
                 mkdir -p ${env_statistic_folder}
             ############## configure scenario ############
                 if [ "$j" == "smallGroup" ]
                 then
                   send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 120 -S -g ${maxConnectionOption}`
                 else if [ "$j" == "bigGroup" ]
                      then
                        send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 120 -S -G ${maxConnectionOption}`
                      fi
                 fi
cat << EOF > $JobConfig
serviceType: $tag
transportType: ${Transport}
hubProtocol: ${MessageEncoding}
scenario: ${Scenario}
EOF
                 cd $ScriptWorkingDir
                 if [ "$j" == "smallGroup" ]
                 then
                   python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -g -d ${sigbench_run_duration} ${maxConnectionOption}>>$JobConfig
                 else if [ "$j" == "bigGroup" ]
                      then
                        python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -G -d ${sigbench_run_duration} ${maxConnectionOption}>>$JobConfig
                      fi
                 fi
                 cat << EOF >> $JobConfig
serverUrl: ${serverUrl}
messageSize: ${bench_send_size}
EOF
                 ############## run bench #####################
                 run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString
               done
             else 
               scenario_folder=${tag}_${Transport}_${MessageEncoding}_${Scenario}   ## take care of the gen of this folder which is also required by gen_html.sh
               # do not forget the tailing '/'
               export env_statistic_folder="${ScriptWorkingDir}/${result_root}/${scenario_folder}/"
               export env_result_folder=$env_statistic_folder                                         # tell the dotnet program where to save counters.txt
               mkdir -p ${env_statistic_folder}
               ############## configure scenario ############
               send=`python gen_complex_pipeline.py -t $Transport -s $Scenario -u unit${unit} -d 120 -S ${maxConnectionOption}`
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
messageSize: ${bench_send_size}
EOF
             ############## run bench #####################
               run_and_gen_report $connectStr $tag $Scenario $Transport $MessageEncoding $connection $concurrentConnection $send $ConnectionString
             fi
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

# global env:
# bench_serviceunit_list, ScriptWorkingDir, result_root
# copy_syslog, copy_nginx_log
# specify connection string as input parameter
function run_unit() {
   local ConnectionString="$1"
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
   run_benchmark $service
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
   create_asrs $DogFoodResouceGroup $signalrServiceName $Sku $service
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
   ######
   
   delete_signalr_service $signalrServiceName $DogFoodResouceGroup
done

}
# require global env:
# ASRSEnv, DogFoodResouceGroup, ASRSLocation
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
create_group_if_not_exist $DogFoodResouceGroup $ASRSLocation
}

# global env: ScriptWorkingDir, DogFoodResouceGroup, ASRSEnv
function clean_ASRS_group() {
############# remove SignalR Service Resource Group #########
cd $ScriptWorkingDir
delete_group $DogFoodResouceGroup
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

  nohup ${VMMgrDir}/JenkinsScript --PidFile='./pid/pid_remove_rsg.txt' --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal &
}

## register exit handler to remove resource group ##
# global env:
# CurrentWorkingDir VMMgrDir
function register_exit_handler() {
  cd $CurrentWorkingDir
  dotnet publish -c Release -f netcoreapp2.1 -o ${VMMgrDir}  --self-contained -r linux-x64
  trap remove_resource_group EXIT
}
