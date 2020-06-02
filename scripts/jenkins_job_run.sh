#!/bin/bash

function jenkins_job_run() {
    # input parameters
    local resourceGroupPrefix=$1
    local vmImageId=$2
    local vmUser=$3
    local vmPassword=$4
    local vmLocation=$5
    local clientVmCount=$6
    local serverVmCount=$7
    local nginx_root=$8

    # import scripts
    source ./jenkins_functions.sh
    
    # set root path
    set_global_env $WORKSPACE

    # generate random prefix of vm and resource grooup
    resourceGroupPrefix="SignalRPerformance"
    postfix=`date +%Y%m%d%H%M%S`
    vmPrefix="${resourceGroupPrefix}${postfix}"

    # configure agent
    generate_vm_provison_config $vmImageId ${vmUser} ${vmPassword} ${vmPrefix} ${vmLocation} ${clientVmCount} ${serverVmCount}

    # create VMs
    dotnet run -p $RootFolder/JenkinsScript.csproj -- --PidFile="pid_create_vm.txt" --step=CreateAllVmsInSameVnet \
    --VnetGroupName=$BenchServerGroup \
    --VnetName=$VnetName \
    --SubnetName=$SubnetName \
    --AgentConfigFile=$AgentConfig \
    --DisableRandomSuffix \
    --ServicePrincipal=$ServicePrincipal

    cat $PrivateIps
    cat $PublicIps

    # override the default environment
cat << EOF >jenkins_env.sh
nginx_root=$nginx_root
sigbench_run_duration=$sigbench_run_duration
EOF

    set_global_env $WORKSPACE
    set_job_env

    # register exit handler to remove resource group ##
    register_exit_handler

    # run for all units
    prepare_ASRS_creation

    run_all_units ${vmUser} ${vmPassword}

    ############# cleanup mail ##########
    if [ -e /tmp/send_mail.txt ]
    then
    rm /tmp/send_mail.txt
    fi

    ## generate report
    gen_final_report
}

