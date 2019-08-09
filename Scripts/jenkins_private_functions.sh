#!/bin/bash

. ./func_env.sh

declare -A ScenarioHandlerDict=(["frequentJoinLeaveGroup"]="SendToGroup" ["sendToGroup"]="SendToGroup" ["restSendToGroup"]="SendToGroup" ["restPersistSendToGroup"]="SendToGroup" ["sendToClient"]="SendToClient")

function clean_known_hosts()
{
  echo "" > ~/.ssh/known_hosts
}

function run_command_core()
{
  local tag=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local user=$5
  local passwd="$6"
  local connectionString="$7"
  local outputDir="$8"
  local config_path=$9
  local connection=${10}
  local concurrentConnection=${11}
  local send=${12}
  local serverUrl=${13}
  local unit=${14}
  run_command $user $passwd $connectionString $outputDir $config_path $unit $Scenario
  cd $ScriptWorkingDir
  #### generate the connection configuration for HTML ####
cat << EOF > ${cmd_config_prefix}_${MessageEncoding}_${Scenario}_${tag}_${Transport}
connection=${connection}
connection_concurrent=${concurrentConnection}
send=${send}
bench_config_endpoint="$serverUrl"
EOF

  ## gen_html.sh requires bench_name_list, bench_codec_list, and bench_type_list
  export bench_name_list="$Scenario"
  export bench_codec_list="$MessageEncoding"
  export bench_type_list="${tag}_${Transport}"
  disable_exit_immediately_when_fail
  sh gen_html.sh $connectionString
  enable_exit_immediately_when_fail
}

function isUseAzureWeb()
{
  if [ "$AspNetSignalR" == "true" ] || [ "$AzWebSignalR" == "true" ]
  then
    echo 1
  else
    echo 0
  fi
}

function get_reduced_appserverCount()
{
  local unit=$1
  local scenario=$2
  local appserverInUse=200
  local useAzureWeb=$(isUseAzureWeb)
  # for none AspNet, we cannot surpass serverVmCount,
  # but for AspNet, we ignore serverVmCount because we use Azure WebApp
  if [ $useAzureWeb -ne 1 ] && [ "$serverVmCount" != "" ]
  then
    appserverInUse=$serverVmCount
  fi
  # handle AspNet app server
  if [ $useAzureWeb -eq 1 ] && [ "$AspNetWebAppCount" != "" ]
  then
    appserverInUse=$AspNetWebAppCount
  fi
  if [ "$DisableReduceServer" != "true" ]
  then
    local limitedAppserver
    if [ $useAzureWeb -ne 1 ]
    then
      limitedAppserver=`python get_appserver_count.py -u $unit -s $scenario`
    else
      limitedAppserver=`python get_appserver_count.py -u $unit -q webappserver -s $scenario`
    fi
    if [ $limitedAppserver -lt $appserverInUse ]
    then
       appserverInUse=$limitedAppserver
    fi
  else
    echo "!! you use $appserverInUse app servers !!"
  fi
  echo $appserverInUse
}

function get_reduced_appserverUrl()
{
  local unit=$1
  local scenario=$2
  local appserverInUse=$(get_reduced_appserverCount $unit $scenario)
  local appserverUrls=`python extract_ip.py -i $PublicIps -q appserverPub -c $appserverInUse`
  echo $appserverUrls
}

function createWebApp()
{
  local unit=$1
  local appPrefix=$2
  local connectionString="$3"
  local serverUrlOutFile=$4
  local appPlanIdOutFile=$5
  local webAppIdOutFile=$6
  local appPlanScaleOutFile=$7
  local scenario=$8

  local resGroup=$AspNetWebAppResGrp #"${appPrefix}"`date +%H%M%S`
  local appserverCount=$(get_reduced_appserverCount $unit $scenario)
  local gitRepo=$GitRepo
  disable_exit_immediately_when_fail
  cd $AspNetWebMgrWorkingDir
  dotnet run -- deploy --servicePrincipal $ServicePrincipal \
              --location ${VMLocation} \
              --webappNamePrefix "${appPrefix}" \
              --webappCount $appserverCount \
              --connectionString "$connectionString" \
              --outputFile $serverUrlOutFile --resourceGroup $resGroup \
              --appServicePlanIdOutputFile $appPlanIdOutFile \
              --appServicePlanScaleOutputFile $appPlanScaleOutFile \
              --webAppIdOutputFile $webAppIdOutFile \
              --githubRepo $gitRepo
  enable_exit_immediately_when_fail
}

function collectWebAppMetrics()
{
  local appPlanOut=$1
  local webAppOut=$2
  local outputDir=$3
  local duration=$4
  cd $WebAppMonitorWorkingDir
  local i
  for i in `cat $appPlanOut`
  do
    local webname=`echo $i|awk -F / '{print $NF}'`
    dotnet run -- --secondsBeforeNow $duration --servicePrincipal $ServicePrincipal --resourceId $i > $outputDir/${webname}_appPlan_metrics.txt
  done
  for i in `cat $webAppOut`
  do
    local webname=`echo $i|awk -F / '{print $NF}'`
    dotnet run -- --secondsBeforeNow $duration --servicePrincipal $ServicePrincipal --resourceId $i > $outputDir/${webname}_webApp_metrics.txt
  done
}

function downloadWebAppLog()
{
  local webAppOut=$1
  local outputDir=$2
  local i
  if [ "$AspNetSignalR" != "true" ] # AspNet SignalR has not writen log to filesystem
  then
    for i in `cat $webAppOut`
    do
      local webname=`echo $i|awk -F / '{print $NF}'`
      $AspNetWebMgrDir/DeployWebApp downloadLog --servicePrincipal $ServicePrincipal --WebAppResourceId $i --LocalFilePrefix $outputDir/webappserverlog
    done
    cd $outputDir
    tar zcvf webappserverlog.log.tgz webappserverlog*.log
    rm webappserverlog*.log
    cd -
  fi
}

function gen4AspNet()
{
  local configPath=$1
  sed -i 's/CreateConnection/CreateAspNetConnection/g' $configPath
  sed -i 's/Reconnect/AspNetReconnect/g' $configPath
}

function normalizeSendSize()
{
  local send_size=$1
  local ms=2048
  if [ "$send_size" != "" ]
  then
     local re='^[0-9]+$'
     if [[ $send_size =~ $re ]] ; then
        ms=$send_size
     fi
  fi
  echo $ms
}

function normalizeSendInterval()
{
  local send_interval=$1
  local interval=1000
  if [ "$send_interval" != "" ]
  then
     local re='^[0-9]+$'
     if [[ $send_interval =~ $re ]] ; then
        interval=$send_interval
     fi
  fi
  echo $interval
}
# global parameters:
#   kind,
#   bench_send_size, sigbench_run_duration, useMaxConnection
#   ToleratedMaxConnectionFailCount
#   ToleratedMaxConnectionFailPercentage
#   ToleratedMaxLatencyPercentage
function GenBenchmarkConfig()
{
  local unit=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local appserverUrls=$5
  local groupType=$6
  local configPath=$7
  local connectionString="$8"
  local sendSize="$9"

  local maxConnectionOption=""
  if [ "$useMaxConnection" == "true" ]
  then
    maxConnectionOption="-m"
  fi
  local sz=$bench_send_size
  if [ "$sendSize" != "None" ]
  then
    sz=$sendSize
  fi
  local ms=$(normalizeSendSize $sz)
  local interval=$(normalizeSendInterval $send_interval)

  local groupTypeOp
  local toleratedConnDropCountOp
  local toleratedConnDropPercentageOp
  local toleratedMaxLatencyPercentageOp
  if [ "$ToleratedMaxConnectionFailCount" != "" ]
  then
    toleratedConnDropCountOp="-cc $ToleratedMaxConnectionFailCount"
  fi
  if [ "$ToleratedMaxConnectionFailPercentage" != "" ]
  then
    toleratedConnDropPercentageOp="-cp $ToleratedMaxConnectionFailPercentage"
  fi
  if [ "$ToleratedMaxLatencyPercentage" != "" ]
  then
    toleratedMaxLatencyPercentageOp="-cs $ToleratedMaxLatencyPercentage"
  fi
  if [ "$groupType" != "None" ]
  then
    groupTypeOp="-g $groupType"
  fi
  local settings=settings.yaml
  local connectionTypeOption="-ct Core"
  if [ "$AspNetSignalR" == "true" ]
  then
     settings=aspnet_settings.yaml
     connectionTypeOption="-ct AspNet"
  else if [[ "$Scenario" == "rest"* ]]
       # it is rest API scenario
       then
            connectionTypeOption="-ct CoreDirect"
            appserverUrls="$connectionString"
       fi
  fi
  local benchKind="perf"
  if [ "$kind" == "longrun" ]
  then
     benchKind="$kind"
  fi
  python3 generate.py -u $unit -S $Scenario \
                      -t $Transport -p $MessageEncoding \
                      -U $appserverUrls -d $sigbench_run_duration \
                      $groupTypeOp -ms $ms -i $interval \
                      -c $configPath $maxConnectionOption \
                      -s $settings $connectionTypeOption \
                      -k $benchKind \
                      $toleratedConnDropCountOp $toleratedConnDropPercentageOp $toleratedMaxLatencyPercentageOp
  # display part of the configuration to avoid 'write error: Resource temporarily unavailable'
  head -n 200 $configPath
  echo "......"
}

function RunSendToGroup()
{
  local tag=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local user=$5
  local passwd="$6"
  local connectionString="$7"
  local outputDir="$8"
  local unit=$9
  local groupType=${10}
  local appserverUrls

  local appPrefix serverUrlOut appPlanOut webAppOut appPlanScaleOut

  local maxConnectionOption
  if [ "$useMaxConnection" == "true" ]
  then
    maxConnectionOption="-m"
  fi
  local startSeconds=$SECONDS

  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    cd $ScriptWorkingDir
    appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
  else
    if [ "$AspNetSignalR" == "true" ]
    then
       appPrefix="aspnetwebapp"
    else
       appPrefix="aspnetcorewebapp"
    fi
    if [ "$NeverStopAppServer" == "true" ]
    then
       serverUrlOut=$JenkinsRootPath/${appPrefix}.txt
       appPlanOut=$JenkinsRootPath/${appPrefix}_appPlan.txt
       webAppOut=$JenkinsRootPath/${appPrefix}_webApp.txt
       appPlanScaleOut=$JenkinsRootPath/${appPrefix}_appPlanScaleOut.txt
       if [ ! -e $serverUrlOut ]
       then
          createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
       fi
    else
       serverUrlOut=$outputDir/${appPrefix}.txt
       appPlanOut=$outputDir/${appPrefix}_appPlan.txt
       webAppOut=$outputDir/${appPrefix}_webApp.txt
       appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt
       createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
    fi
    if [ -e $serverUrlOut ]
    then
      appserverUrls=`cat $serverUrlOut`
    else
      echo "!!Fail to create web app!!"
      return
    fi
    startSeconds=$SECONDS
  fi

  cd $PluginScriptWorkingDir
  local config_path=$outputDir/${tag}_${Scenario}_${Transport}_${MessageEncoding}.config
  GenBenchmarkConfig $unit $Scenario $Transport $MessageEncoding $appserverUrls $groupType $config_path "$connectionString" None
  local connection=`python3 get_sending_connection.py -g $groupType -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q totalConnections $maxConnectionOption`
  local concurrentConnection=`python3 get_sending_connection.py -g $groupType -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q concurrentConnection $maxConnectionOption`
  local send=`python3 get_sending_connection.py -g $groupType -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q sendingSteps $maxConnectionOption`
  run_command_core $tag $Scenario $Transport $MessageEncoding $user "$passwd" "$connectionString" $outputDir $config_path $connection $concurrentConnection $send $appserverUrls $unit
  if [ $useAzureWeb -eq 1 ]
  then
    local duration=$(($SECONDS-$startSeconds))
    # get the metrics
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # download load
    downloadWebAppLog $webAppOut $outputDir
    # remove appserver
    if [ "$NeverStopAppServer" != "true" ]
    then
       $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
    fi
  fi
}

function RunSendToClient()
{
  local tag=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local user=$5
  local passwd="$6"
  local connectionString="$7"
  local outputDir="$8"
  local unit=$9
  local msgSize=${10}
  local appserverUrls
  local maxConnectionOption
  if [ "$useMaxConnection" == "true" ]
  then
    maxConnectionOption="-m"
  fi

  local appPrefix serverUrlOut appPlanOut webAppOut appPlanScaleOut
  local startSeconds=$SECONDS
  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    cd $ScriptWorkingDir
    appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
  else
    if [ "$AspNetSignalR" == "true" ]
    then
       appPrefix="aspnetwebapp"
    else
       appPrefix="aspnetcorewebapp"
    fi
    if [ "$NeverStopAppServer" == "true" ]
    then
       serverUrlOut=$JenkinsRootPath/${appPrefix}.txt
       appPlanOut=$JenkinsRootPath/${appPrefix}_appPlan.txt
       webAppOut=$JenkinsRootPath/${appPrefix}_webApp.txt
       appPlanScaleOut=$JenkinsRootPath/${appPrefix}_appPlanScaleOut.txt
       if [ ! -e $serverUrlOut ]
       then
          createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
       fi
    else
       serverUrlOut=$outputDir/${appPrefix}.txt
       appPlanOut=$outputDir/${appPrefix}_appPlan.txt
       webAppOut=$outputDir/${appPrefix}_webApp.txt
       appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt
       createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
    fi
    if [ -e $serverUrlOut ]
    then
      appserverUrls=`cat $serverUrlOut`
    else
      echo "!!Fail to create web app!!"
      return
    fi
    startSeconds=$SECONDS
  fi

  cd $PluginScriptWorkingDir
  local config_path=$outputDir/${tag}_${Scenario}_${Transport}_${MessageEncoding}.config
  GenBenchmarkConfig $unit $Scenario $Transport $MessageEncoding $appserverUrls None $config_path "$connectionString" $msgSize
  local connection=`python3 get_sending_connection.py -ms $msgSize -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q totalConnections $maxConnectionOption`
  local concurrentConnection=`python3 get_sending_connection.py -ms $msgSize -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q concurrentConnection $maxConnectionOption`
  local send=`python3 get_sending_connection.py -ms $msgSize -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q sendingSteps $maxConnectionOption`

  run_command_core $tag $Scenario $Transport $MessageEncoding $user "$passwd" "$connectionString" $outputDir $config_path $connection $concurrentConnection $send $appserverUrls $unit
  if [ $useAzureWeb -eq 1 ]
  then
    local duration=$(($SECONDS-$startSeconds))
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # download load
    downloadWebAppLog $webAppOut $outputDir
    # remove appserver
    if [ "$NeverStopAppServer" != "true" ]
    then
       $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
    fi
  fi
}

function RunCommonScenario()
{
  local tag=$1
  local Scenario=$2
  local Transport=$3
  local MessageEncoding=$4
  local user=$5
  local passwd="$6"
  local connectionString="$7"
  local outputDir="$8"
  local unit=$9
  local appserverUrls
  local maxConnectionOption
  if [ "$useMaxConnection" == "true" ]
  then
    maxConnectionOption="-m"
  fi
  local appPrefix serverUrlOut appPlanOut webAppOut appPlanScaleOut
  local startSeconds=$SECONDS
  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    if [[ "$Scenario" == "rest"* ]]
    then
       appserverUrls="ignored" ## rest API does not need app server
    else
       cd $ScriptWorkingDir
       appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
    fi
  else
    if [ "$AspNetSignalR" == "true" ]
    then
       appPrefix="aspnetwebapp"
    else
       appPrefix="aspnetcorewebapp"
    fi
    if [ "$NeverStopAppServer" == "true" ]
    then
       serverUrlOut=$JenkinsRootPath/${appPrefix}.txt
       appPlanOut=$JenkinsRootPath/${appPrefix}_appPlan.txt
       webAppOut=$JenkinsRootPath/${appPrefix}_webApp.txt
       appPlanScaleOut=$JenkinsRootPath/${appPrefix}_appPlanScaleOut.txt
       if [ ! -e $serverUrlOut ]
       then
          createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
       fi
    else
       serverUrlOut=$outputDir/${appPrefix}.txt
       appPlanOut=$outputDir/${appPrefix}_appPlan.txt
       webAppOut=$outputDir/${appPrefix}_webApp.txt
       appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt
       createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
    fi

    if [ -e $serverUrlOut ]
    then
      appserverUrls=`cat $serverUrlOut`
    else
      echo "!!Fail to create web app!!"
      return
    fi
    startSeconds=$SECONDS
  fi

  cd $PluginScriptWorkingDir
  local config_path=$outputDir/${tag}_${Scenario}_${Transport}_${MessageEncoding}.config
  GenBenchmarkConfig $unit $Scenario $Transport $MessageEncoding $appserverUrls None $config_path "$connectionString" None
  local connection=`python3 get_sending_connection.py -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q totalConnections $maxConnectionOption`
  local concurrentConnection=`python3 get_sending_connection.py -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q concurrentConnection $maxConnectionOption`
  local send=`python3 get_sending_connection.py -u $unit -S $Scenario -t $Transport -p $MessageEncoding -q sendingSteps $maxConnectionOption`
  run_command_core $tag $Scenario $Transport $MessageEncoding $user "$passwd" "$connectionString" $outputDir $config_path $connection $concurrentConnection $send $appserverUrls $unit

  if [ $useAzureWeb -eq 1 ]
  then
    local duration=$(($SECONDS-$startSeconds))
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # download load
    downloadWebAppLog $webAppOut $outputDir
    # remove appserver
    if [ "$NeverStopAppServer" != "true" ]
    then
       $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
    fi
  fi
}

function SendToGroup()
{
  local Scenario=$1
  local Transport=$2
  local MessageEncoding=$3
  local origTag=$4
  local user=$5
  local passwd="$6"
  local connectStr="$7"
  local unit=$8
  local j tag
  for j in $GroupTypeList
  do
     tag=${origTag}"_${j}"

     prepare_result_folder_4_scenario $tag $Transport $MessageEncoding $Scenario

     start_collect_top_for_signalr_and_nginx

     RunSendToGroup $tag $Scenario $Transport $MessageEncoding $user $passwd "$connectStr" $env_statistic_folder $unit $j

     stop_collect_top_for_signalr_and_nginx

     copy_log_from_k8s

     reboot_all_pods "$connectStr"
  done
}

function SendToClient()
{
  local Scenario=$1
  local Transport=$2
  local MessageEncoding=$3
  local origTag=$4
  local user=$5
  local passwd="$6"
  local connectStr="$7"
  local unit=$8
  local j tag
  for j in $sendToClientMsgSize
  do
     tag=${origTag}"_${j}"

     prepare_result_folder_4_scenario $tag $Transport $MessageEncoding $Scenario

     start_collect_top_for_signalr_and_nginx

     RunSendToClient $tag $Scenario $Transport $MessageEncoding $user $passwd "$connectStr" $env_statistic_folder $unit $j

     stop_collect_top_for_signalr_and_nginx

     copy_log_from_k8s

     reboot_all_pods "$connectStr"
  done
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

function start_collect_top_for_signalr_and_nginx()
{
    local k8s_result_dir=$env_statistic_folder
    local monitorDuration=$(($sigbench_run_duration * $MaxSendIteration))
    cd $ScriptWorkingDir
    . ./func_env.sh
    . ./kubectl_utils.sh
    local service_name=$(extract_servicename_from_connectionstring $ConnectionString)
    if [ "$service_name" != "" ]
    then
       nohup sh collect_pod_top.sh $service_name $k8s_result_dir $monitorDuration &
       collect_pod_top_pid=$!
       if [ "$g_nginx_ns" != "" ]
       then
          nohup sh collect_nginx_top.sh $service_name $g_nginx_ns $k8s_result_dir $monitorDuration &
          collect_nginx_top_pid=$!
       fi
       nohup sh collect_connections.sh $service_name $k8s_result_dir $monitorDuration &
       collect_conn_pid=$!
    fi
}

function stop_collect_top_for_signalr_and_nginx()
{
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
    if [ "$collect_conn_pid" != "" ]
    then
       local a=`ps -o pid= -p $collect_conn_pid`
       if [ "$a" != "" ]
       then
          kill $collect_conn_pid
       fi
    fi
}

function copy_log_from_k8s()
{
    local k8s_result_dir=$env_statistic_folder
    cd $ScriptWorkingDir
    . ./func_env.sh
    . ./kubectl_utils.sh
    local service_name=$(extract_servicename_from_connectionstring $ConnectionString)
    disable_exit_immediately_when_fail
    if [ "$service_name" != "" ]
    then
    ############# copy pod log ############
       copy_syslog $service_name $k8s_result_dir
       get_nginx_cpu_info $service_name "$g_nginx_ns" $k8s_result_dir
       get_nginx_log $service_name "$g_nginx_ns" $k8s_result_dir
       get_nginx_pod_detail $service_name "$g_nginx_ns" $k8s_result_dir
       get_k8s_cpu_info $service_name $k8s_result_dir
       get_k8s_pod_status $service_name $k8s_result_dir
    fi
    enable_exit_immediately_when_fail
}

function reboot_all_pods()
{
   local connectionString=$1
   cd $ScriptWorkingDir
   . ./kubectl_utils.sh
   disable_exit_immediately_when_fail
   local service_name=$(extract_servicename_from_connectionstring $connectionString)
   if [ "$service_name" != "" ] && [ "$RebootASRS" != "false" ]
   then
     restart_all_pods $service_name
   fi
   enable_exit_immediately_when_fail
}

function run_on_scenario() {
  local Scenario=$1
  local Transport=$2
  local MessageEncoding=$3
  local origTag=$4
  local user=$5
  local passwd="$6"
  local connectStr="$7"
  local unit=$8
  if [ "${ScenarioHandlerDict[$Scenario]}" != "" ]
  then
     eval "${ScenarioHandlerDict[$Scenario]} $Scenario $Transport $MessageEncoding $origTag $user $passwd \"$connectStr\" $unit"
  else
     prepare_result_folder_4_scenario $origTag $Transport $MessageEncoding $Scenario
     start_collect_top_for_signalr_and_nginx
     run_and_gen_report $origTag $Scenario $Transport $MessageEncoding $user $passwd "$connectStr" $env_statistic_folder $unit
     stop_collect_top_for_signalr_and_nginx
     copy_log_from_k8s
     reboot_all_pods "$connectStr"
  fi
  mark_error_if_failed "$origTag"
}

function mark_error_if_failed()
{
  local tag="$1"
  local counterPath=`find ${env_statistic_folder} -iname "counters.txt"`
  if [ "$counterPath" == "" ]
  then
     gMeetError="${gMeetError} $tag"
  fi
}

function mark_job_as_failure_if_meet_error()
{
  local exitCode=$1
  if [ "$gMeetError" != "" ]; then
     echo "!!!! Failed for ${gMeetError}, so mark this job as failure !!!!"
     exit 1
  fi
  if [ $exitCode -ne 0 ]; then
     exit $exitCode
  fi
}

# global environment:
# AspNetSignalR
function run_benchmark() {
  local unit=$1
  local user=$2
  local passwd="$3"
  local connectStr="$4"
  local tag="unit"$unit
  local useAzureWeb=$(isUseAzureWeb)
  if [ "$AspNetSignalR" == "true" ]
  then
    tag="AspNet"$tag
  else
    if [ $useAzureWeb -eq 1 ]
    then
      tag="AzWeb"$tag
    fi
  fi
  if [ "kind" == "longrun" ]
  then
    tag="Longrun"$tag
  fi
  local Scenario
  local Transport
  local MessageEncoding
  
  for Scenario in $bench_scenario_list
  do
      for Transport in $bench_transport_list
      do
         for MessageEncoding in $bench_encoding_list
         do
            run_on_scenario $Scenario $Transport $MessageEncoding $tag $user $passwd "$connectStr" $unit
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
  local unit=$9

  RunCommonScenario $tag $Scenario $Transport $MessageEncoding $user $passwd "$connectionString" $outputDir $unit
}

function build_rpc_master() {
  local targetDir=$1
  local tmpMaster=/tmp/master
  if [ -e $tmpMaster ]
  then
     rm -rf $tmpMaster
  fi
  cd $PluginRpcBuildWorkingDir
  ./build.sh master $tmpMaster
  if [ ! -e $tmpMaster/master ]
  then
    echo "!!! Fail to build master: the '$tmpMaster/master' does not exist!!!"
    exit 1
  fi
  if [ -e $targetDir/publish ]
  then
     rm -rf $targetDir/publish
  fi
  mv $tmpMaster $targetDir/publish
  cd $targetDir
  tar zcvf publish.tgz publish
  rm -rf publish
  cd -
}

function build_rpc_agent() {
  local targetDir=$1
  local tmpAgent=/tmp/agent
  if [ -e $tmpAgent ]
  then
     rm -rf $tmpAgent
  fi
  cd $PluginRpcBuildWorkingDir
  ./build.sh agent $tmpAgent
  if [ ! -e $tmpAgent/agent ]
  then
    echo "!!! Fail to build agent: the '$tmpAgent/agent' does not exist!!!"
    exit 1
  fi
  if [ -e $targetDir/publish ]
  then
     rm -rf $targetDir/publish
  fi
  mv $tmpAgent $targetDir/publish
  cd $targetDir
  tar zcvf publish.tgz publish
  rm -rf publish
  cd -
}

build_app_server() {
  local targetDir=$1
  local tmpAppServer=/tmp/appserver
  if [ -e $tmpAppServer ]
  then
     rm -rf $tmpAppServer
  fi
  cd $AppServerWorkingDir
  ./build.sh $tmpAppServer
  if [ ! -e $tmpAppServer/appserver ]
  then
    echo "!!! Fail to build appserver: the '$tmpAppServer/appserver' does not exist!!!"
    exit 1
  fi
  if [ -e $targetDir/publish ]
  then
     rm -rf $targetDir/publish
  fi
  mv $tmpAppServer $targetDir/publish
  cd $targetDir
  tar zcvf publish.tgz publish
  rm -rf publish
  cd -
}

start_collect_agents_appserver_top()
{
  local user=$1
  local passwd="$2"
  local outputDir="$3"
  local script_collect_agents_top="collect_agents_top.sh"
  local script_collect_appserver_top="collect_appserver_top.sh"
  cd $ScriptWorkingDir
cat << EOF > $script_collect_agents_top
#!/bin/bash
while [ true ]
do
  for i in `python extract_ip.py -i $PrivateIps -q agentList`
  do
    date_time=\`date --iso-8601='seconds'\`
    echo "\${date_time} " >> $outputDir/agent_\${i}_top.txt
    sshpass -p "$passwd" ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@\${i} "top -b -n 1|head -n 17" >> $outputDir/agent_\${i}_top.txt
  done
  sleep 1
done
EOF
cat << EOF > $script_collect_appserver_top
#!/bin/bash
while [ true ]
do
  for i in `python extract_ip.py -i $PrivateIps -q appserverList`
  do
    date_time=\`date --iso-8601='seconds'\`
    echo "\${date_time} " >> $outputDir/appserver_\${i}_top.txt
    sshpass -p "$passwd" ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@\${i} "top -b -n 1|head -n 17" >> $outputDir/appserver_\${i}_top.txt
    event=\`sshpass -p "$passwd" ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@\${i} "curl http://169.254.169.254/metadata/scheduledevents?api-version=2017-08-01 -H \"Metadata\"=\"true\"" 2>/dev/null\`
    echo "\${date_time} \${event}">> $outputDir/appserver_\${i}_schedule.txt
  done
  sleep 1
done
EOF
  nohup sh $script_collect_agents_top &
  collect_agents_top_pid=$!
  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    nohup sh $script_collect_appserver_top &
    collect_appserver_top_pid=$!
  fi
}

stop_collect_agents_appserver_top()
{
  if [ "$collect_agents_top_pid" != "" ]
  then
    # kill the process if it is alive
    local a=`ps -o pid= -p $collect_agents_top_pid`
    if [ "$a" != "" ]
    then
       kill $collect_agents_top_pid
    fi
  fi
  if [ "$collect_appserver_top_pid" != "" ]
  then
    # kill the process if it is alive
    local a=`ps -o pid= -p $collect_appserver_top_pid`
    if [ "$a" != "" ]
    then
       kill $collect_appserver_top_pid
    fi
  fi
}

function copy_log_from_agents_master()
{
  local user=$1
  local passwd="$2"
  local outputDir="$3"
  cd $ScriptWorkingDir
  local i j k
  for i in `python extract_ip.py -i $PrivateIps -q agentList`
  do
    #sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:/home/$user/agent/publish/agent*.log $outputDir/ # only 1 log was left
    #if [ $? -ne 0 ]
    #then
    #  sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/agent -iname agent*.log"
    #fi
    local agentLogPath=`sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/agent -iname agent*.log"`
    k=0
    for j in $agentLogPath
    do
      sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:${j} $outputDir/agent_${i}_${k}.log
      if [ $? -ne 0 ]
      then
        sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/agent -iname agent*.log"
      fi
      k=$(($k+1))
    done
  done
  for i in `python extract_ip.py -i $PrivateIps -q master`
  do
    sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:/home/$user/master/publish/master*.log $outputDir/
    if [ $? -ne 0 ]
    then
      sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/master -iname master*.log"
    fi
  done
  cd $outputDir
  tar zcvf agent_master_log.tgz agent*.log master*.log
  rm agent*.log master*.log
  cd -
}

function run_command() {
  local user=$1
  local passwd="$2"
  local connectionString="$3"
  local outputDir="$4"
  local configPath=$5
  local unit=$6
  local scenario=$7
  local notStartAppServer=0
  local startAppServerOption
  local appserverInUse
  local appserver
  local appserverDir

  cd $ScriptWorkingDir
  local master=`python extract_ip.py -i $PrivateIps -q master`
  local agents=`python extract_ip.py -i $PrivateIps -q agents`
  local masterDir=$CommandWorkingDir/master
  local agentDir=$CommandWorkingDir/agent
  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    if [[ "$Scenario" != "rest"* ]]
    then
      appserverInUse=$(get_reduced_appserverCount $unit $scenario)
      appserver=`python extract_ip.py -i $PrivateIps -q appserver -c $appserverInUse`
      appserverDir=$CommandWorkingDir/appserver
      mkdir -p $appserverDir
      build_app_server $appserverDir
      startAppServerOption="--AppServerHostnames=$appserver --AppserverProject=$appserverDir --AppserverTargetPath=/home/${user}/appserver.tgz --AppServerCount=$appserverInUse"
    else
      notStartAppServer=1
    fi
  else
    notStartAppServer=1
  fi
  mkdir -p $masterDir
  mkdir -p $agentDir
  build_rpc_master $masterDir
  build_rpc_agent $agentDir
  cd $CommandWorkingDir
  local remoteCmd="remove_counters.sh"
  cat << EOF > $remoteCmd
#!/bin/bash
if [ -d /home/${user}/master ]
then
  cd /home/${user}/master
  a=\`find . -iname counters.txt\`
  if [ "\$a" != "" ]
  then
    rm \$a
  fi
fi
EOF
  sshpass -p ${passwd} scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $remoteCmd ${user}@${master}:/home/${user}/
  sshpass -p ${passwd} ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "chmod +x $remoteCmd"
  sshpass -p ${passwd} ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "./$remoteCmd"
  disable_exit_immediately_when_fail
  start_collect_agents_appserver_top ${user} $passwd ${outputDir}
  #try_catch_netstat_when_server_conn_drop ${user} $passwd "$connectionString"
  cd $CommandWorkingDir
  # "never stop app server" is used for long run stress test
  local neverStopAppServerOp
  if [ "$NeverStopAppServer" == "true" ]
  then
    neverStopAppServerOp="--NotStopAppServer=1"
  fi
  local useAzureWeb=$(isUseAzureWeb)
  if [ $useAzureWeb -ne 1 ]
  then
    dotnet run -- --RpcPort=5555 --Username=$user --Password=$passwd \
         --AgentList="$agents" --MasterHostname="$master" $startAppServerOption \
         --MasterProject="$masterDir" \
         --MasterTargetPath="/home/${user}/master.tgz" \
         --AgentProject="$agentDir" \
         --AgentTargetPath="/home/${user}/agent.tgz" \
         --BenchmarkConfiguration="$configPath" \
         --BenchmarkConfigurationTargetPath="/home/${user}/signalr.yaml" \
         --AzureSignalRConnectionString="$connectionString" \
         --AppserverLogDirectory="${outputDir}" \
         --NotStartAppServer=$notStartAppServer $neverStopAppServerOp
  else
    dotnet run -- --RpcPort=5555 --AgentList="$agents" --MasterHostname="$master" \
               --Username=$user --Password=$passwd \
               --MasterProject="$masterDir" \
               --AgentProject="$agentDir" \
               --AgentTargetPath="/home/${user}/agent.tgz" \
               --MasterTargetPath="/home/${user}/master.tgz" \
               --BenchmarkConfiguration="$configPath" \
               --BenchmarkConfigurationTargetPath="/home/${user}/signalr.yaml" \
               --AzureSignalRConnectionString="$connectionString" \
               --NotStartAppServer=1
  fi
  stop_collect_agents_appserver_top ${user} $passwd ${outputDir}
  local counterPath=`sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "find /home/${user}/master -iname counters.txt"`
  sshpass -p ${passwd} scp -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master}:$counterPath ${outputDir}/
  if [ $? -ne 0 ]
  then
    sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "find /home/${user}/master -iname counters.txt"
  fi
  copy_log_from_agents_master ${user} $passwd ${outputDir}
  enable_exit_immediately_when_fail
}

function create_asrs()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unit=$4

. ./az_signalr_service.sh
. ./kubectl_utils.sh

  local signalr_service
  if [ "$separatedRedis" != "" ] && [ "$separatedRouteRedis" != "" ] && [ "$separatedAcs" != "" ] && [ "$separatedIngressVMSS" != "" ]
  then
   if [ "$ServiceMode" == "" ]
   then
     signalr_service=$(create_asrs_with_acs_redises $rsg $name $sku $unit $separatedRedis $separatedRouteRedis $separatedAcs $separatedIngressVMSS)
   else
     if [ "$ServiceMode" == "Serverless" ]
     then
        signalr_service=$(create_serverless_asrs_with_acs_redises $rsg $name $ASRSLocation $unit $separatedRedis $separatedRouteRedis $separatedAcs $separatedIngressVMSS)
     fi
   fi
  else
   if [ "$separatedRedis" != "" ] && [ "$separatedAcs" != "" ] && [ "$separatedIngressVMSS" != "" ]
   then
      signalr_service=$(create_signalr_service_with_specific_acs_vmset_redis $rsg $name $sku $unit $separatedRedis $separatedAcs $separatedIngressVMSS)
   else
    if [ "$separatedIngressVMSS" != "" ] && [ "$separatedAcs" != "" ]
    then
      signalr_service=$(create_signalr_service_with_specific_ingress_vmss $rsg $name $sku $unit $separatedAcs $separatedIngressVMSS)
    else
     if [ "$separatedRedis" != "" ] && [ "$separatedAcs" != "" ]
     then
      signalr_service=$(create_signalr_service_with_specific_acs_and_redis $rsg $name $sku $unit $separatedRedis $separatedAcs)
     else
      if [ "$separatedRedis" != "" ]
      then
        signalr_service=$(create_signalr_service_with_specific_redis $rsg $name $sku $unit $separatedRedis)
      else
        if [ "$ServiceMode" == "" ]
        then
           signalr_service=$(create_signalr_service $rsg $name $sku $unit)
        else
           if [ "$ServiceMode" == "Serverless" ]
           then
              signalr_service=$(create_serverless_signalr_service $rsg $name $ASRSLocation $unit)
           fi
        fi
      fi
     fi
    fi
   fi
  fi
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
  local exitStatus=$?
  echo "!!Received EXIT!! and remove all created VMs, exit code: $exitStatus"
  local clean_resource_daemon=daemon_${NORMALIZED_JOB_NAME}_cleanresource

  (daemonize -v -o /tmp/${clean_resource_daemon}.out -e /tmp/${clean_resource_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh $CleanResourceScript &) && (mark_job_as_failure_if_meet_error $exitStatus)
}
