#! /bin/bash

# variable section
# modify variable in this section, do not modify other script if you don't what you are doing 
username=<USERNAME>
password=<PASSWORD>
benchmark_path=<BENCHMARK_CONFIGURATION_PATH>
master_hostname=<MASTER_HOSTNAME>
slave_list=<SLAVE_HOSTNAME_A>:<RPC_PORT_A>,<SLAVE_HOSTNAME_B>:<RPC_PORT_B>
app_server_hostnames=<APP_SERVER_HOSTNAME_A>,<APP_SERVER_HOSTNAME_B>
remote_store_root=<REMOTE_STORE_ROOT_FOR_EXE_AND_CONFIG>
azure_signalr_connection_string=<AZURE_SIGNALR_CONNECTION_STRING>
# end of variable section


project_root=`pwd`"/.."

current_dir=`pwd`
if [[ $benchmark_path != /* ]]; then
  benchmark_path=$current_dir"/"$benchmark_path
fi

echo $benchmark_path

# install sshpass
if dpkg-query -l 'sshpass' 
then
    echo ''
else 
    echo 'Install sshpass'
    sudo apt-get install -y sshpass
fi

# build app server
cd $project_root/SignalRServiceBenchmarkPlugin/utils/AppServer
dotnet build

# generate rpc protocol
cd $project_root/SignalRServiceBenchmarkPlugin/framework/rpc/
. ./util.sh
generate_proto

# build slave
cd $project_root/SignalRServiceBenchmarkPlugin/framework/slave
dotnet build

# build master
cd $project_root/SignalRServiceBenchmarkPlugin/framework/master
dotnet build


# run automation tool
cd $project_root/SignalRServiceBenchmarkPlugin/utils/Commander
dotnet run -- \
--SlaveList=$slave_list \
--MasterHostname=$master_hostname \
--AppServerHostnames=$app_server_hostnames \
--Username=$username \
--Password=$password \
--AppserverProject=$project_root/SignalRServiceBenchmarkPlugin/utils/AppServer \
--MasterProject=$project_root/SignalRServiceBenchmarkPlugin/framework/master/ \
--SlaveProject=$project_root/SignalRServiceBenchmarkPlugin/framework/slave/ \
--BenchmarkConfiguration=$benchmark_path \
--AppserverTargetPath=$remote_store_root/appserver/publish.tgz \
--MasterTargetPath=$remote_store_root/master/publish.tgz \
--SlaveTargetPath=$remote_store_root/slave/publish.tgz \
--BenchmarkConfigurationTargetPath=$remote_store_root/benchmark_config.yaml \
--AzureSignalRConnectionString="$azure_signalr_connection_string" \
--UserMode

# copy counters to current machine
cd $current_dir
sshpass -p $password scp $username@$master_hostname:$remote_store_root/master/publish/publish/counters.txt counters.txt

# generate report
cd $current_dir
report_simple_dist_linux/report_simple/report_simple $current_dir"/counters.txt"
