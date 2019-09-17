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
   export PluginScriptWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/scripts/tools/ASRSConfigGenerator/
   export PluginRpcBuildWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/src/rpc/
   export ScriptWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/scripts/                     # folders to find all scripts
   export CurrentWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/scripts/tools/AzureVMMgr     # workding directory
   export CommandWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/src/utils/Commander
   export AppServerWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/src/appserver
   export AspNetWebMgrWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/src/utils/DeployWebApp
   export WebAppMonitorWorkingDir=$Jenkins_Workspace_Root/${relative_dir}/src/utils/WebAppMonitor
############# those configurations are shared in Jenkins folder #####
   export AgentConfig=$JenkinsRootPath'/agent.yaml'
   export PrivateIps=$JenkinsRootPath'/privateIps.yaml'
   export PublicIps=$JenkinsRootPath'/publicIps.yaml'
   export ServicePrincipal=$JenkinsRootPath'/servicePrincipal.yaml'

############ global static variables #########
   export RootFolder=$CurrentWorkingDir # some jekins already refers this
   #export ResultFolderSuffix='suffix'
   export VMMgrDir=/tmp/VMMgr
   export AspNetWebMgrDir=/tmp/AspNetWebMgr
   set_job_env
}

function write_az_credentials_to_create_vm() {
  cd $ScriptWorkingDir
  . ./utils.sh
  az_signalr_dev_credentials $ServicePrincipal
  cd -
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
   if [ "$GitRepo" == "" ]
   then
     export GitRepo="https://github.com/clovertrail/AspNetServer"
   else
     export GitRepo=$GitRepo
   fi
   export ASRSResourceGroup="hzatpf"$result_root
   export SignalrServiceName="atpf"${result_root} #-`date +%H%M%S`
   if [ "$kind" == "" ] || [ "$kind" == "perf" ]
   then
     export AspNetWebAppResGrp="hzperfwebapp"$result_root
   else
     export AspNetWebAppResGrp="hzlongrunwebapp"$result_root
   fi
   local jobName=`echo "${JOB_NAME}"|tr ' ' '_'`
   export NORMALIZED_JOB_NAME=${jobName}
   export CleanResourceScript=/tmp/clean_resource_${jobName}_${result_root}.sh # every job has its own script to clean resource, they will never execute concurrently.
   export MaxSendIteration=120 # we evaluate the total running time per this value
   record_build_info # record the jenkins job to /tmp/send_mail.txt
   prebuild_helper_tool
   write_az_credentials_to_create_vm
   generate_clean_resource_script $CleanResourceScript
}

# global var: $AgentConfig
function generate_vm_provison_config() {
   local img=$1
   local VMUser=$2
   local VMPassword="$3"
   local vmPrefix=$4
   local VMLocation=$5
   local clientVmCount=$6
   local serverVmCount=$7

cat << EOF > $AgentConfig
rpcPort: 5555
sshPort: 22

imageId: $img

user: ${VMUser}
password: ${VMPassword}

# config for creating VMs
prefix: ${vmPrefix}
location: ${VMLocation}

# agents (the first one will be master)
agentVmSize: StandardDS2V2
agentVmCount: ${clientVmCount}

# app server
appSvrVmSize: StandardF4sV2
appSvrVmCount: ${serverVmCount}
EOF

}

function generate_clean_resource_script() {
   local script_file=$1
   # I found several daemonize commands can not be executed sometimes, so I merged them to avoid this issue
cat << EOF > $script_file
${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal

if [ "$AspNetSignalR" == "true" ] || [ "$AzWebSignalR" == "true" ]
then
  ${AspNetWebMgrDir}/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal=$ServicePrincipal
fi

cd $ScriptWorkingDir
. ./az_signalr_service.sh

if [ "$ASRSEnv" == "dogfood" ]
then
  az_login_ASRS_dogfood
  delete_group $ASRSResourceGroup
  unregister_signalr_service_dogfood
else
  az_login_signalr_dev_sub
  delete_group $ASRSResourceGroup
fi
EOF

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

function prebuild_helper_tool() {
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
  cd -
}

function register_exit_handler() {
  disable_exit_immediately_when_fail
  trap remove_resource_group EXIT
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

function run_all_units_on_exsiting_webapp()
{
  local user=$1
  local passwd="$2"
  local webappRawUrlList="$3"
  for service in $bench_serviceunit_list
  do
    run_benchmark_on_exsiting_webapp $service ${user} "${passwd}" ${webappRawUrlList}
  done
}
