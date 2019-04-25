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

function get_reduced_appserverCount()
{
  local unit=$1
  local scenario=$2
  local appserverInUse=200
  # for none AspNet, we cannot surpass serverVmCount,
  # but for AspNet, we ignore serverVmCount because we use Azure WebApp
  if [ "$AspNetSignalR" != "true" ] && [ "$serverVmCount" != "" ]
  then
    appserverInUse=$serverVmCount
  fi
  # handle AspNet app server
  if [ "$AspNetSignalR" == "true" ] && [ "$AspNetWebAppCount" != "" ]
  then
    appserverInUse=$AspNetWebAppCount
  fi
  if [ "$DisableReduceServer" != "true" ]
  then
    local limitedAppserver
    if [ "$AspNetSignalR" != "true" ]
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
              --webAppIdOutputFile $webAppIdOutFile
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

  local appPrefix="aspnetwebapp"
  local serverUrlOut=$outputDir/${appPrefix}.txt
  local appPlanOut=$outputDir/${appPrefix}_appPlan.txt
  local webAppOut=$outputDir/${appPrefix}_webApp.txt
  local appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt

  local maxConnectionOption
  if [ "$useMaxConnection" == "true" ]
  then
    maxConnectionOption="-m"
  fi
  local startSeconds=$SECONDS

  if [ "$AspNetSignalR" != "true" ]
  then
    cd $ScriptWorkingDir
    appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
  else
    createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
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
  if [ "$AspNetSignalR" == "true" ]
  then
    local duration=$(($SECONDS-$startSeconds))
    # get the metrics
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # remove appserver
    $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
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

  local appPrefix="aspnetwebapp"
  local serverUrlOut=$outputDir/${appPrefix}.txt
  local appPlanOut=$outputDir/${appPrefix}_appPlan.txt
  local webAppOut=$outputDir/${appPrefix}_webApp.txt
  local appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt
  local startSeconds=$SECONDS
  if [ "$AspNetSignalR" != "true" ]
  then
    cd $ScriptWorkingDir
    appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
  else
    createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
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
  if [ "$AspNetSignalR" == "true" ]
  then
    local duration=$(($SECONDS-$startSeconds))
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # remove appserver
    $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
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
  local appPrefix="aspnetwebapp"
  local serverUrlOut=$outputDir/${appPrefix}.txt
  local appPlanOut=$outputDir/${appPrefix}_appPlan.txt
  local webAppOut=$outputDir/${appPrefix}_webApp.txt
  local appPlanScaleOut=$outputDir/${appPrefix}_appPlanScaleOut.txt
  local startSeconds=$SECONDS
  if [ "$AspNetSignalR" != "true" ]
  then
    if [[ "$Scenario" == "rest"* ]]
    then
       appserverUrls="ignored" ## rest API does not need app server
    else
       cd $ScriptWorkingDir
       appserverUrls=$(get_reduced_appserverUrl $unit $Scenario)
    fi
  else
    createWebApp $unit $appPrefix "$connectionString" $serverUrlOut $appPlanOut $webAppOut $appPlanScaleOut $Scenario
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

  if [ "$AspNetSignalR" == "true" ]
  then
    local duration=$(($SECONDS-$startSeconds))
    collectWebAppMetrics $appPlanOut $webAppOut $outputDir $duration
    # remove appserver
    $AspNetWebMgrDir/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal $ServicePrincipal
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
  if [ "$gMeetError" != "" ]
  then
     echo "!!!! Failed for ${gMeetError}, so mark this job as failure !!!!"
     exit 1
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
  if [ "$AspNetSignalR" == "true" ]
  then
    tag="AspNet"$tag
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

function build_rpc_slave() {
  local targetDir=$1
  local tmpSlave=/tmp/slave
  if [ -e $tmpSlave ]
  then
     rm -rf $tmpSlave
  fi
  cd $PluginRpcBuildWorkingDir
  ./build.sh slave $tmpSlave
  if [ ! -e $tmpSlave/slave ]
  then
    echo "!!! Fail to build slave: the '$tmpSlave/slave' does not exist!!!"
    exit 1
  fi
  if [ -e $targetDir/publish ]
  then
     rm -rf $targetDir/publish
  fi
  mv $tmpSlave $targetDir/publish
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
  if [ ! -e $tmpAppServer/AppServer ]
  then
    echo "!!! Fail to build appserver: the '$tmpAppServer/AppServer' does not exist!!!"
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

try_catch_netstat_when_server_conn_drop()
{
  local user=$1
  local passwd="$2"
  local connectionString="$3"
  local netstat_check_file="netstat_check.sh"
  local remote_netstat_log="netstat.log"
  local asrs_endpoint=`echo "$connectionString"|awk -F \; '{print $1}'|awk -F = '{print $2}'`
cat << EOF > $netstat_check_file
#!/bin/bash
if [ -e $remote_netstat_log ]
then
  rm $remote_netstat_log
fi

appserver_log=\`find . -iname "appserver.log"\`
if [ "\$appserver_log" == "" ]
then
  exit 0
fi

if [ -e $remote_netstat_log ]
then
  rm $remote_netstat_log
fi
i=0
max=3600
while [ \$i -lt \$max ]
do
  conn_drop=\`grep "service was dropped" \$appserver_log\`
  if [ "\$conn_drop" != "" ]
  then
     date_time=\`date --iso-8601='seconds'\`
     echo "\${date_time} " >> $remote_netstat_log
     curl -I $asrs_endpoint 2>/dev/null|head -n 3 >> $remote_netstat_log
     echo "------------------" >> $remote_netstat_log
     curl -I http://www.bing.com 2>/dev/null|head -n 3 >> $remote_netstat_log
  fi
  sleep 1
  i=\$((\$i+1))
done
EOF
  local i
  for i in `python extract_ip.py -i $PrivateIps -q appserverList`
  do
    sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $netstat_check_file $user@${i}:/home/$user/
    sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "chmod +x $netstat_check_file"
    sshpass -p $passwd ssh -f -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "killall $netstat_check_file; nohup ./$netstat_check_file &"
  done
}

fetch_netstat_for_server_conn_drop()
{
  local user=$1
  local passwd="$2"
  local outputDir=$3
  local i
  local remote_netstat_log="netstat.log"
  for i in `python extract_ip.py -i $PrivateIps -q appserverList`
  do
    local netstatLogPath=`sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find . -iname $remote_netstat_log"`
    if [ "$netstatLogPath" != "" ]
    then
      sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:$netstatLogPath $outputDir/appserver_netstat_${i}.txt
      if [ -e $outputDir/appserver_netstat_${i}.txt ]
      then
         cd $outputDir
         tar zcvf appserver_netstat_${i}.txt.tgz appserver_netstat_${i}.txt
         cd -
      fi
    fi
  done
}

start_collect_slaves_appserver_top()
{
  local user=$1
  local passwd="$2"
  local outputDir="$3"
  local script_collect_slaves_top="collect_slaves_top.sh"
  local script_collect_appserver_top="collect_appserver_top.sh"
  cd $ScriptWorkingDir
cat << EOF > $script_collect_slaves_top
#!/bin/bash
while [ true ]
do
  for i in `python extract_ip.py -i $PrivateIps -q slaveList`
  do
    date_time=\`date --iso-8601='seconds'\`
    echo "\${date_time} " >> $outputDir/slave_\${i}_top.txt
    sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@\${i} "top -b -n 1|head -n 17" >> $outputDir/slave_\${i}_top.txt
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
    sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@\${i} "top -b -n 1|head -n 17" >> $outputDir/appserver_\${i}_top.txt
  done
  sleep 1
done
EOF
  nohup sh $script_collect_slaves_top &
  collect_slaves_top_pid=$!
  if [ "$AspNetSignalR" != "true" ]
  then
    nohup sh $script_collect_appserver_top &
    collect_appserver_top_pid=$!
  fi
}

stop_collect_slaves_appserver_top()
{
  if [ "$collect_slaves_top_pid" != "" ]
  then
    # kill the process if it is alive
    local a=`ps -o pid= -p $collect_slaves_top_pid`
    if [ "$a" != "" ]
    then
       kill $collect_slaves_top_pid
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

function copy_log_from_slaves_master()
{
  local user=$1
  local passwd="$2"
  local outputDir="$3"
  cd $ScriptWorkingDir
  local i j k
  for i in `python extract_ip.py -i $PrivateIps -q slaveList`
  do
    #sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:/home/$user/slave/publish/slave*.log $outputDir/ # only 1 log was left
    #if [ $? -ne 0 ]
    #then
    #  sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/slave -iname slave*.log"
    #fi
    local slaveLogPath=`sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/slave -iname slave*.log"`
    k=0
    for j in $slaveLogPath
    do
      sshpass -p $passwd scp -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i}:${j} $outputDir/slave_${i}_${k}.log
      if [ $? -ne 0 ]
      then
        sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR $user@${i} "find /home/$user/slave -iname slave*.log"
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
  tar zcvf slave_master_log.tgz slave*.log master*.log
  rm slave*.log master*.log
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
  local slaves=`python extract_ip.py -i $PrivateIps -q slaves`
  local masterDir=$CommandWorkingDir/master
  local slaveDir=$CommandWorkingDir/slave
  if [ "$AspNetSignalR" != "true" ]
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
  mkdir -p $slaveDir
  build_rpc_master $masterDir
  build_rpc_slave $slaveDir
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
  start_collect_slaves_appserver_top ${user} $passwd ${outputDir}
  #try_catch_netstat_when_server_conn_drop ${user} $passwd "$connectionString"
  cd $CommandWorkingDir
  # "never stop app server" is used for long run stress test
  local neverStopAppServerOp
  if [ "$NeverStopAppServer" == "true" ]
  then
    neverStopAppServerOp="--NotStopAppServer=1"
  fi

  if [ "$AspNetSignalR" != "true" ]
  then
    dotnet run -- --RpcPort=5555 --Username=$user --Password=$passwd \
         --SlaveList="$slaves" --MasterHostname="$master" $startAppServerOption \
         --MasterProject="$masterDir" \
         --MasterTargetPath="/home/${user}/master.tgz" \
         --SlaveProject="$slaveDir" \
         --SlaveTargetPath="/home/${user}/slave.tgz" \
         --BenchmarkConfiguration="$configPath" \
         --BenchmarkConfigurationTargetPath="/home/${user}/signalr.yaml" \
         --AzureSignalRConnectionString="$connectionString" \
         --AppserverLogDirectory="${outputDir}" \
         --NotStartAppServer=$notStartAppServer $neverStopAppServerOp
  else
    dotnet run -- --RpcPort=5555 --SlaveList="$slaves" --MasterHostname="$master" \
               --Username=$user --Password=$passwd \
               --MasterProject="$masterDir" \
               --SlaveProject="$slaveDir" \
               --SlaveTargetPath="/home/${user}/slave.tgz" \
               --MasterTargetPath="/home/${user}/master.tgz" \
               --BenchmarkConfiguration="$configPath" \
               --BenchmarkConfigurationTargetPath="/home/${user}/signalr.yaml" \
               --AzureSignalRConnectionString="$connectionString" \
               --NotStartAppServer=1
  fi
  stop_collect_slaves_appserver_top ${user} $passwd ${outputDir}
  local counterPath=`sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "find /home/${user}/master -iname counters.txt"`
  sshpass -p ${passwd} scp -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master}:$counterPath ${outputDir}/
  if [ $? -ne 0 ]
  then
    sshpass -p $passwd ssh -o StrictHostKeyChecking=no -o LogLevel=ERROR ${user}@${master} "find /home/${user}/master -iname counters.txt"
  fi
  copy_log_from_slaves_master ${user} $passwd ${outputDir}
  #fetch_netstat_for_server_conn_drop ${user} $passwd ${outputDir}
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
   signalr_service=$(create_asrs_with_acs_redises $rsg $name $sku $unit $separatedRedis $separatedRouteRedis $separatedAcs $separatedIngressVMSS)
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
        signalr_service=$(create_signalr_service $rsg $name $sku $unit)
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
  echo "!!Received EXIT!! and remove all created VMs"
  cd $CurrentWorkingDir
  local clean_aspwebapp_daemon=daemon_${JOB_NAME}_cleanwebapp
  local clean_vm_daemon=daemon_${JOB_NAME}_cleanvms
  local clean_asrs_daemon=daemon_${JOB_NAME}_cleanasrs
  ## remove webapp if they are not removed
cat << EOF > /tmp/clean_webapp.sh
${AspNetWebMgrDir}/DeployWebApp removeGroup --resourceGroup=${AspNetWebAppResGrp} --servicePrincipal=$ServicePrincipal
EOF
  daemonize -v -o /tmp/${clean_aspwebapp_daemon}.out -e /tmp/${clean_aspwebapp_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh /tmp/clean_webapp.sh &
  ## remove all test VMs
  local pid_file_path=/tmp/${result_root}_pid_remove_rsg.txt
cat << EOF > /tmp/clean_vms.sh
${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal
${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal
${VMMgrDir}/JenkinsScript --step=DeleteResourceGroupByConfig --AgentConfigFile=$AgentConfig --DisableRandomSuffix --ServicePrincipal=$ServicePrincipal
EOF
  daemonize -v -o /tmp/${clean_vm_daemon}.out -e /tmp/${clean_vm_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh /tmp/clean_vms.sh &
  ## remove ASRS
cat << EOF > /tmp/clean_asrs.sh
cd $ScriptWorkingDir
. ./az_signalr_service.sh

if [ "$ASRSEnv" == "dogfood" ]
then
  az_login_ASRS_dogfood
  delete_group $ASRSResourceGroup
  delete_group $ASRSResourceGroup
  unregister_signalr_service_dogfood
else
  az_login_signalr_dev_sub
  delete_group $ASRSResourceGroup
  delete_group $ASRSResourceGroup
fi
EOF
  daemonize -v -o /tmp/${clean_asrs_daemon}.out -e /tmp/${clean_asrs_daemon}.err -E BUILD_ID=dontKillcenter /usr/bin/nohup /bin/sh /tmp/clean_asrs.sh &
  mark_job_as_failure_if_meet_error
}
