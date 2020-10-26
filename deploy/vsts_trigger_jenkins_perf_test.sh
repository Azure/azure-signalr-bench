#!/bin/bash
function vsts_trigger_jenkins_perf_test {
    # do NOT support scenario list
    # only support one variable changes

    local group_types=(`echo $1`)  # list
    local run_indexes=(`echo $2`) # list
    local build_url=$3
    local user_name=$4
    local api_token=$5
    local asrs_location=$6
    local units=(`echo $7`) # list
    local scenarios=(`echo $8`)  # list
    local transport_types=(`echo $9`)  # list
    local protocols=(`echo ${10}`)  # list
    local message_sizes=(`echo ${11}`) # list
    local vm_location=${12}
    local vm_image=${13}
    local vnet_resource_group=${14}
    local vnet_name=${15}
    local vnet_subnet=${16}
    local client_vm_count=(`echo ${17}`)  # list
    local server_vm_count=(`echo ${18}`)  # list
    local build_type=${19}
    local release_name=${20}
    local release_id=${21}
    local aspnet=${22}
    local service_mode=${23}

    for ms in ${message_sizes[*]}; do \
    for ts in ${transport_types[*]}; do \
    for gp in ${group_types[*]}; do \
    for i in ${run_indexes[*]}; do curl  $build_url \
    --user "$user_name:$api_token" \
    --data ASRSLocation="$asrs_location" \
    --data bench_serviceunit_list="${units[i]}" \
    --data bench_scenario_list="$scenarios" \
    --data bench_transport_list="$ts" \
    --data bench_encoding_list="$protocols" \
    --data sendToClientMsgSize="$ms" \
    --data VMLocation="$vm_location" \
    --data GlobalVmImageId="$vm_image" \
    --data BenchServerGroup="$vnet_resource_group" \
    --data VnetName="$vnet_name" \
    --data SubnetName="$vnet_subnet" \
    --data clientVmCount="${client_vm_count[i]}" \
    --data serverVmCount="${server_vm_count[i]}" \
    --data build_type="$build_type" \
    --data GroupTypeList="$gp" \
    --data AspNetSignalR="$aspnet" \
    --data sigbench_run_duration="60" \
    --data ServiceMode="$service_mode" \
    --data build_title_extra="Release Name: ${release_name} ID: ${release_id}" ; done; done; done; done;
}