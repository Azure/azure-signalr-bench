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
   export CommandWorkingDir=$Jenkins_Workspace_Root/SignalRServiceBenchmarkPlugin/utils/Commander
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
}

# depends on set_global_env
function set_job_env() {
   export result_root=`date +%Y%m%d%H%M%S`
   export DogFoodResourceGroup="hzatpf"`date +%M%S`
   export serverUrl=`awk '{print $2}' $JenkinsRootPath/JobConfig.yaml`
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

function run_command() {
  local user=$1
  local passwd="$2"
  cd $ScriptWorkingDir
  local master=`python extract_ip.py -i $PrivateIps -q master`
  local appserver=`python extract_ip.py -i $PrivateIps -q appserver`
  local slaves=`python extract_ip.py -i $PrivateIps -q slaves`
  cd $CommandWorkingDir
  dotnet run -- --RpcPort=5555 --SlaveList="$slaves" --MasterHostname="$master" --AppServerHostnames="$appserver" \
         --Username="$user" --Password="$passwd" \
         --AppserverProject="/home/wanl/executables/appserver" \
         --MasterProject="/home/wanl/executables/master" \
         --SlaveProject="/home/wanl/executables/slave" \
         --AppserverTargetPath="/home/wanl/appserver.zip" --MasterTargetPath="/home/wanl/master.zip" \
         --SlaveTargetPath="/home/wanl/slave.zip" \
         --BenchmarkConfiguration="/home/wanl/benchmarkConfiguration/echo.yaml" \
         --BenchmarkConfigurationTargetPath="/home/wanl/signalr.yaml" \
         --AzureSignalRConnectionString="Endpoint=https://signalr-wanl-benchmark-v3.service.signalr.net;AccessKey=4DB3v7sgkv+5mz8e+plPjA/JZPCYOBV4pDIJLH7tgng=;Version=1.0;"
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
  BUILD_ID=dontKillcenter /usr/bin/nohup ${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal &

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
