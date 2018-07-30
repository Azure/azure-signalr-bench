#!/bin/bash
. ./utils.sh

prepare_bench_client() {
 local host=$1
 local user=$2
 local port=$3
 local ansible_root_dir=$4
 local host_file=autogen_install_bench_cli
  cd $ansible_root_dir
cat << EOF > $host_file
[linux]
client ansible_host=$host ansible_port=$port ansible_user=${user}
EOF
 ansible-playbook -i $host_file pam_limits.yaml
 ansible-playbook -i $host_file install_config_go.yaml
 ansible-playbook -i $host_file git_clone_websocket.yaml
 ansible-playbook -i $host_file build_websocket.yaml
 cd -
}

change_sshd_port() {
 local host=$1
 local user=$2
 local ansible_root_dir=$3
 local host_file=autogen_sshd_port_hosts

 cd $ansible_root_dir
cat << EOF > $host_file
[linux]
client ansible_host=$host ansible_port=22 ansible_user=${user}
EOF
 ansible-playbook -i $host_file change_sshd_port.yaml
 cd -
}

list_vm_public_ip() {
 local vmName=$1
 local rsg=$2
 az vm list-ip-addresses -g $rsg \
                         -n $vmName|jq ".[].virtualMachine.network.publicIpAddresses[0].ipAddress"|tr -d '"'
}

user_pubkey_update_all_vms() {
  local username=$1
  local pub_key_file=$2
  local rsg=$3
  az vm user update -u $username \
                    --ssh-key-value "$(< $pub_key_file)" \
                    --ids $(az vm list -g $rsg --query "[].id" -o tsv)
}

add_nsg_ports_for_all() {
  local vmName=$1
  local rsg=$2
  local new_ssh_port=$3
  local signalr_service_ports="5001-5003"
  local web_client_port="7000"
  local signalr_hub_port="5050"

  az vm open-port --port $signalr_service_ports --resource-group $rsg --name $vmName --priority 900
  az vm open-port --port $new_ssh_port --resource-group $rsg --name $vmName --priority 901
  az vm open-port --port $web_client_port --resource-group $rsg --name $vmName --priority 902
  az vm open-port --port $signalr_hub_port --resource-group $rsg --name $vmName --priority 903
}

create_resource_group() {
  local res_grp=$1
  local location=$2
  local isExisting=$(az group exists --name $res_grp 2>&1)
  if [ "$isExisting" != "true" ]
  then
    az group create --name $res_grp --location $location
  else
    return
  fi
  isExisting=$(az group exists --name $res_grp 2>&1)
  if [ "$isExisting" != "true" ]
  then
    echo 1
  else
    echo 0
  fi
}

create_vm() {
  local vmName=$1
  local vmImage=$2
  local location=$3
  local admin=$4
  local sshPubKeyFile=$5
  local vmSize=$6
  local rsg=$7
  local dns=$8
  local isExisting=$(az group exists --name $rsg 2>&1)
  if [ "$isExisting" != "true" ]
  then
    echo 1
    return
  fi
  az vm create  \
     --resource-group $rsg \
     --name $vmName        \
     --image $vmImage      \
     --size $vmSize        \
     --location $location  \
     --admin-username $admin \
     --ssh-key-value $sshPubKeyFile \
     --public-ip-address-dns-name $dns \
     --no-wait
}

get_vm_img_resource_id() {
  local rsg=$1
  local img_name=$2
  local resource_id=`az image show -g $rsg -n $img_name -o=json|jq ".id"|tr -d '"'`
  echo "$resource_id"
}

get_vm_img_location() {
  local rsg=$1
  local img_name=$2
  local location=`az image show -g $rsg -n $img_name -o=json|jq ".location"|tr -d '"'`
  echo "$location"
}

create_vm_from_img_no_wait() {
  local rsg=$1
  local vm_name=$2
  local user=$3
  local pub_key_file=$4
  local vm_size=$5
  local location=$6
  local resource_id=$7
  az vm create --resource-group $rsg \
               --name $vm_name \
               --image "$resource_id" \
               --admin-username $user --ssh-key-value $pub_key_file \
               --public-ip-address-dns-name $vm_name \
               --size $vm_size \
               --location $location \
               --no-wait
}

deallocate_vm() {
  local rsg=$1
  local vm=$2
  az vm deallocate --resource-group $rsg --name $vm
}

generalize_vm() {
  local rsg=$1
  local vm=$2
  az vm generalize --resource-group $rsg --name $vm
}

create_image() {
  local rsg=$1
  local vm=$2
  local img=$3
  az image create --resource-group $rsg --name $img --source $vm
}
