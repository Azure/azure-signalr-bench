#!/bin/bash

. ./func_env.sh
# input Jenkinw workspace directory
# this function is invoked in every job entry
function set_global_env() {
   local relative_dir="azure-signalr-bench"
   local Jenkins_Workspace_Root=$1
   if [ $# -eq 2 ]
   then
      relative_dir=$2
   fi
   export JenkinsRootPath="$Jenkins_Workspace_Root"
   export ScriptWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/Scripts                     # folders to find all scripts
   export CurrentWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/v2/JenkinsScript/     # workding directory
   export CommandWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/utils/Commander
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
   export VMMgrDir=/tmp/VMMgr
   export nginx_root=/mnt/Data/NginxRoot
   export g_nginx_ns="ingress-nginx"
}

# depends on set_global_env
function set_job_env() {
   export result_root=`date +%Y%m%d%H%M%S`
   export DogFoodResourceGroup="hzatpf"`date +%M%S`
   export serverUrl=`awk '{print $2}' $JenkinsRootPath/JobConfig.yaml`
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

function register_exit_handler() {
  cd $CurrentWorkingDir
  if [ -d ${VMMgrDir} ]
  then
    rm -rf ${VMMgrDir}
  fi
  dotnet publish -c Release -f netcoreapp2.1 -o ${VMMgrDir} --self-contained -r linux-x64
  trap remove_resource_group EXIT
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

function prepare_result_folder_4_scenario()
{
   local tag=$1
   local Transport=$2
   local MessageEncoding=$3
   local Scenario=$4
   ## take care of the gen of this folder which is also required by gen_html.sh
   local scenario_folder=${tag}_${Transport}_${MessageEncoding}_${Scenario}
   export env_statistic_folder=${ScriptWorkingDir}/${result_root}/${scenario_folder}
   
   mkdir -p ${env_statistic_folder}
}

function run_all_units() {
 local user=$1
 local passwd="$2"
 local service
 local signalrServiceName
 for service in $bench_serviceunit_list
 do
   cd $ScriptWorkingDir
   ConnectionString="" # set it to be invalid first
   signalrServiceName="atpf"`date +%H%M%S`
   create_asrs $DogFoodResourceGroup $signalrServiceName $service
   if [ "$ConnectionString" == "" ]
   then
     echo "Skip the running on SignalR service unit'$service' since it was failed to create"
     continue
   fi

   run_benchmark $service $user "$passwd" "$ConnectionString"
   
   delete_signalr_service $signalrServiceName $DogFoodResourceGroup
 done
}


function run_benchmark() {
  local unit=$1
  local user=$2
  local passwd="$3"
  local connectStr="$4"
  local tag="unit"$unit
  local Scenario
  local Transport
  local MessageEncoding
  
  for Scenario in $bench_scenario_list
  do
      for Transport in $bench_transport_list
      do
         for MessageEncoding in $bench_encoding_list
         do
            prepare_result_folder_4_scenario $tag $Transport $MessageEncoding $Scenario
            local k8s_result_dir=$env_statistic_folder
            ## TODO start collecting top
            cd $ScriptWorkingDir
            . ./func_env.sh
            . ./kubectl_utils.sh
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
            ## TODO generate configuration
            run_and_gen_report $tag $Scenario $Transport $MessageEncoding $user $passwd "$connectStr" $k8s_result_dir
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
               copy_syslog $service_name $k8s_result_dir
               get_nginx_log $service_name "$g_nginx_ns" $k8s_result_dir
               get_k8s_pod_status $service_name $k8s_result_dir
            fi
            ## TODO reboot ASRS
         done
      done
  done

}

function run_and_gen_report() {
  local tag=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local user=$5
  local passwd="$6"
  local connectionString="$7"
  local outputDir="$8"
  #TODO dummy values
  local connection=3000
  local concurrentConnection=300
  local send=1000
  
  run_command $user $passwd $connectionString $outputDir
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
  sh gen_html.sh $connectionString
}

function run_command() {
  local user=$1
  local passwd="$2"
  local connectionString="$3"
  local outputDir="$4"
  cd $ScriptWorkingDir
  local master=`python extract_ip.py -i $PrivateIps -q master`
  local appserver=`python extract_ip.py -i $PrivateIps -q appserver`
  local slaves=`python extract_ip.py -i $PrivateIps -q slaves`
  local appserverUrls=`python extract_ip.py -i $PublicIps -q appserverPub`

  python parse_config.py -i /home/wanl/benchmarkConfiguration/echo.yaml -s $appserverUrls > echo.yaml
  cd $CommandWorkingDir
  local remoteCmd="remove_counters.sh"
  cat << EOF > $remoteCmd
#!/bin/bash
if [ -e counters.txt ]
then
  rm counters.txt
fi
EOF
  sshpass -p ${passwd} scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $remoteCmd ${user}@${master}:/home/${user}/
  sshpass -p ${passwd} ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "chmod +x $remoteCmd"
  sshpass -p ${passwd} ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "./$remoteCmd"
  dotnet run -- --RpcPort=5555 --SlaveList="$slaves" --MasterHostname="$master" --AppServerHostnames="$appserver" \
         --Username=$user --Password=$passwd \
         --AppserverProject="/home/wanl/executables/appserver" \
         --MasterProject="/home/wanl/executables/master" \
         --SlaveProject="/home/wanl/executables/slave" \
         --AppserverTargetPath="/home/${user}/appserver.tgz" --MasterTargetPath="/home/${user}/master.tgz" \
         --SlaveTargetPath="/home/${user}/slave.tgz" \
         --BenchmarkConfiguration="$ScriptWorkingDir/echo.yaml" \
         --BenchmarkConfigurationTargetPath="/home/${user}/signalr.yaml" \
         --AzureSignalRConnectionString="$connectionString"
  sshpass -p ${passwd} scp -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master}:/home/${user}/counters.txt ${outputDir}/
}

function create_asrs()
{
  local rsg=$1
  local name=$2
  local unit=$3

. ./az_signalr_service.sh
. ./kubectl_utils.sh

  local signalr_service=$(create_signalr_service $rsg $name "Basic_DS2" $unit)
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
## exit handler to remove resource group ##
# global env:
# CurrentWorkingDir, ServicePrincipal, AgentConfig, VMMgrDir
function remove_resource_group() {
  echo "!!Received EXIT!! and remove all created VMs"

  cd $CurrentWorkingDir
  local clean_vm_daemon=daemon_${JOB_NAME}_cleanvms
  local clean_asrs_daemon=daemon_${JOB_NAME}_cleanasrs
  ## remove all test VMs
  #local pid_file_path=/tmp/${result_root}_pid_remove_rsg.txt
  #BUILD_ID=dontKillcenter /usr/bin/nohup ${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal &

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
  daemonize -v -o /tmp/${clean_asrs_daemon}.out -e /tmp/${clean_asrs_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh /tmp/clean_asrs.sh &
}
