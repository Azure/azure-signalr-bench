#!/bin/bash

. ./jenkins_private_functions.sh
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
   export PluginScriptWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/plugins/Plugin.Microsoft.Azure.SignalR.Benchmark/Scripts/BenchmarkConfigurationGenerator/
   export PluginRpcBuildWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/framework/rpc/
   export ScriptWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/Scripts/                     # folders to find all scripts
   export CurrentWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/Scripts/JenkinsScript/     # workding directory
   export CommandWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/utils/Commander
   export AppServerWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/utils/AppServer
   export AspNetWebMgrWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/utils/DeployWebApp
   export WebAppMonitorWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/SignalRServiceBenchmarkPlugin/utils/WebAppMonitor
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
   export AspNetWebMgrDir=/tmp/AspNetWebMgr
   set_job_env
}

# depends on set_global_env
function set_job_env() {
   if [ "$nginx_root" == "" ]
   then
     export nginx_root=/mnt/Data/NginxRoot
   else
     export nginx_root=$nginx_root
   fi
   if [ "$g_nginx_ns" == "" ]
   then
     export g_nginx_ns="ingress-nginx"
   else
     export g_nginx_ns=$g_nginx_ns
   fi
   if [ "$result_root" == "" ]
   then
     export result_root=`date +%Y%m%d%H%M%S`
   else
     export result_root=$result_root
   fi
   if [ "$Sku" == "" ]
   then
     export Sku="Basic_DS2"
   else
     export Sku=$Sku
   fi
   export ASRSResourceGroup="hzatpf"$result_root
   export SignalrServiceName="atpf"${result_root} #-`date +%H%M%S`
   export AspNetWebAppResGrp="hzperfwebapp"$result_root
   export MaxSendIteration=120 # we evaluate the total running time per this value
   record_build_info # record the jenkins job to /tmp/send_mail.txt
}

function azure_login() {
cd $ScriptWorkingDir
. ./az_signalr_service.sh

if [ "$ASRSEnv" == "dogfood" ]
then
  register_signalr_service_dogfood
  az_login_ASRS_dogfood
else
  az_login_signalr_dev_sub
fi
}

function set_tags_for_production() {
  if [ "$ASRSLocation" == "westus2" ] && [ "$ASRSEnv" == "production" ]
  then
     # on production environment, we use separate Redis for westus2 region
     if [ -e westus2_redis_rowkey.txt ]
     then
       separatedRedis=`cat westus2_redis_rowkey.txt`
     fi
     if [ -e westus2_route_redis_rowkey.txt ]
     then
       separatedRouteRedis=`cat westus2_route_redis_rowkey.txt`
     fi
     if [ -e westus2_acs_rowkey.txt ]
     then
       separatedAcs=`cat westus2_acs_rowkey.txt`
     fi
     if [ -e westus2_vm_set.txt ]
     then
       separatedIngressVMSS=`cat westus2_vm_set.txt`
     fi
  fi
}

# require global env:
# ASRSEnv, ASRSResourceGroup, ASRSLocation
function prepare_ASRS_creation() {
  azure_login
  create_group_if_not_exist $ASRSResourceGroup $ASRSLocation
  set_tags_for_production
}

# global env: ScriptWorkingDir, ASRSResourceGroup, ASRSEnv
function clean_ASRS_group() {
############# remove SignalR Service Resource Group #########
cd $ScriptWorkingDir
delete_group $ASRSResourceGroup
if [ "$ASRSEnv" == "dogfood" ]
then
  unregister_signalr_service_dogfood
fi
}

function register_exit_handler() {
  disable_exit_immediately_when_fail
  trap remove_resource_group EXIT
  cd $CurrentWorkingDir
  if [ -d ${VMMgrDir} ]
  then
    rm -rf ${VMMgrDir}
  fi
  dotnet publish -c Release -f netcoreapp2.1 -o ${VMMgrDir} --self-contained -r linux-x64

  cd $AspNetWebMgrWorkingDir
  if [ -d ${AspNetWebMgrDir} ]
  then
    rm -rf ${AspNetWebMgrDir}
  fi
  dotnet publish -c Release -f netcoreapp2.1 -o ${AspNetWebMgrDir} --self-contained -r linux-x64
  enable_exit_immediately_when_fail
}

function run_unit() {
 local user=$1
 local passwd="$2"
 local ConnectionString="$3"
 local service
 export RebootASRS="false"
 clean_known_hosts
 for service in $bench_serviceunit_list
 do
   cd $ScriptWorkingDir
   run_benchmark $service $user "$passwd" "$ConnectionString"
 done
}

function run_all_units() {
 local user=$1
 local passwd="$2"
 local service
 clean_known_hosts
 for service in $bench_serviceunit_list
 do
   cd $ScriptWorkingDir
   ConnectionString="" # set it to be invalid first
   azure_login
   create_asrs $ASRSResourceGroup $SignalrServiceName $Sku $service
   if [ "$ConnectionString" == "" ]
   then
     echo "Skip the running on SignalR service unit'$service' since it was failed to create"
     continue
   fi
   # wait for the instance to be ready
   sleep 120
   run_benchmark $service $user "$passwd" "$ConnectionString"
 done
 azure_login
 delete_signalr_service $SignalrServiceName $ASRSResourceGroup
}
