#!/bin/bash
. ./utils.sh

ASRS_cloud_name=AzureSignalRServiceDogfood

function register_signalr_service_dogfood() {
  local check_existing=`az cloud list|jq .[].name|grep $ASRS_cloud_name`
  if [ "$check_existing" == "" ]
  then
     az cloud register -n $ASRS_cloud_name \
                  --endpoint-active-directory "https://login.windows-ppe.net" \
                  --endpoint-active-directory-graph-resource-id "https://graph.ppe.windows.net/" \
                  --endpoint-active-directory-resource-id "https://management.core.windows.net/" \
                  --endpoint-gallery "https://gallery.azure.com/" \
                  --endpoint-management "https://umapi-preview.core.windows-int.net/" \
                  --endpoint-resource-manager "https://api-dogfood.resources.windows-int.net/" \
                  --profile "latest"
  fi
  az cloud set -n $ASRS_cloud_name
}

function unregister_signalr_service_dogfood() {
#recover the default cloud
  az cloud set -n AzureCloud
  local check_existing=`az cloud list|jq .[].name|grep $ASRS_cloud_name`
  if [ "$check_existing" != "" ]
  then
     az cloud unregister -n $ASRS_cloud_name
  fi
}

function add_signalr_extension()
{
 # `az extension list |jq .[].name`
  az extension show --name "signalr"
  if [ $? -ne 0 ]
  then
    az extension add -n "signalr"
  fi
}

function create_group_if_not_exist() {
  local resgrp=$1
  local location=$2
  local grps=`az group list -o json|jq .[].name|grep $resgrp`

  if [ "$grps" == "" ] || [ "$grps" != "$resgrp" ]
  then
    az group create --name $resgrp --location $location
  fi
}

function delete_group() {
  local rsg=$1
  az group delete --name $rsg -y --no-wait
}

function create_serverless_asrs_with_acs_redises()
{
  local rsg=$1
  local name=$2
  local location=$3
  local unitCount=$4
  local redisRowKey=$5
  local redisRouteRowKey=$6
  local acsRowKey=$7
  local vmSet=$8
  local signalrHostName
  local p="{'properties':{'features':[{'flag':'ServiceMode','value':'Serverless'}]},'location':'$location','sku':{'name':'Standard_S1','capacity':$unitCount},'tags':{'SIGNALR_MESSAGE_REDIS_ROW_KEY':'$redisRowKey','SIGNALR_ROUTE_REDIS_ROW_KEY':'$redisRouteRowKey','SIGNALR_ACS_ROW_KEY':'$acsRowKey','SIGNALR_INGRESS_VM_SET':'$vmSet'}}"

  local pp=`echo $p|sed "s/'/\"/g"`
  local properties=$pp
  signalrHostName=$(az resource create -g $rsg -n $name \
                                       --namespace Microsoft.SignalRService \
                                       --resource-type SignalR \
                                       --properties $properties \
                                       --is-full-object)
  # the new instance may be killed ~2 times, so please wait
  echo "`date +%Y%m%d%H%M%S`: waiting instance ready"
  sleep 2100
  echo "`date +%Y%m%d%H%M%S`: finish waiting"
  echo "$signalrHostName"
}

function create_serverless_signalr_service()
{
  local rsg=$1
  local name=$2
  local location=$3
  local unitCount=$4
  local p="{'properties':{'features':[{'flag':'ServiceMode','value':'Serverless'}]},'location':'$location','sku':{'name':'Standard_S1','capacity':$unit}}"

  local pp=`echo $p|sed "s/'/\"/g"`
  local properties=$pp
  local ret=$(az resource create -g $rsg -n $name --namespace Microsoft.SignalRService --resource-type SignalR --properties $properties --is-full-object)
  echo "`date +%Y%m%d%H%M%S`: waiting instance ready"
  sleep 2100
  echo "`date +%Y%m%d%H%M%S`: finish waiting"
  echo $ret
}

function create_signalr_service()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     -o tsv)
  echo "$signalrHostName"
}

function create_signalr_service_with_specific_acs_and_redis()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local redisRowKey=$5
  local acsRowKey=$6
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_REDIS_ROW_KEY=$redisRowKey SIGNALR_ACS_ROW_KEY=$acsRowKey \
     -o tsv)
  echo "$signalrHostName"
}

function create_signalr_service_with_specific_ingress_vmss()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local acsRowKey=$5
  local vmSet=$6
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_INGRESS_VM_SET=$vmSet SIGNALR_ACS_ROW_KEY=$acsRowKey \
     -o tsv)
  echo "$signalrHostName"
}

function create_free_asrs_with_acs()
{
  local rsg=$1
  local name=$2
  local sku="Free_F1"
  local unitCount=1
  local acsRowKey=$3
  local signalrHostName
  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_ACS_ROW_KEY=$acsRowKey \
     -o tsv)
  echo "$signalrHostName"
}

function create_asrs_with_acs_redises()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local redisRowKey=$5
  local redisRouteRowKey=$6
  local acsRowKey=$7
  local vmSet=$8
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_MESSAGE_REDIS_ROW_KEY=$redisRowKey \
            SIGNALR_ROUTE_REDIS_ROW_KEY=$redisRouteRowKey \
            SIGNALR_ACS_ROW_KEY=$acsRowKey \
            SIGNALR_INGRESS_VM_SET=$vmSet \
     -o tsv)
  echo "$signalrHostName"
}

function create_signalr_service_with_specific_acs_vmset_redis()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local redisRowKey=$5
  local acsRowKey=$6
  local vmSet=$7
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_MESSAGE_REDIS_ROW_KEY=$redisRowKey SIGNALR_ACS_ROW_KEY=$acsRowKey SIGNALR_INGRESS_VM_SET=$vmSet \
     -o tsv)
  echo "$signalrHostName"
}

function create_signalr_service_with_specific_redis()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local redisRow=$5
  local signalrHostName
  # add extension
  #add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     --tags SIGNALR_MESSAGE_REDIS_ROW_KEY=$redisRow \
     -o tsv)
  echo "$signalrHostName"
}

function check_signalr_service_dns()
{
  local rsg=$1
  local name=$2
  local nslookupData
  local externalIp=`az signalr show -n $name -g $rsg -o=json|jq ".externalIp"|tr -d '"'`
  local hostname=`az signalr list --query [*].hostName --output table|grep "$name"`
  #local hostname=${name}.servicedev.signalr.net
  local end=$((SECONDS + 120))
  while [ $SECONDS -lt $end ]
  do
    #dig $hostname # check the detail reason if nslookup fails
    nslookupdata=`nslookup $hostname|grep "Address:"|grep "$externalIp"`
    if [ "$nslookupdata" != "" ]
    then
      echo 0
      return
    fi
    sleep 1
  done
  echo 1
}

function check_signalr_service()
{
  local target_service=$1
  local existing=`az signalr list --query [*].hostName --output table|grep "$target_service"`
  if [ "$existing" != "" ]
  then
     echo "1"
  else
     echo "0"
  fi
}

function query_connection_string()
{
  local signarl_service_name=$1
  local rsg=$2
  local signalrHostName=`az signalr list --query [*].hostName --output table|grep "$signarl_service_name"`
  if [ "$signalrHostName" == "" ]
  then
     echo ""
     return
  fi
  #local signalrHostName=${signarl_service_name}.servicedev.signalr.net
  local connectionString=`az signalr key list --name $signarl_service_name --resource-group $rsg --query primaryConnectionString -o tsv`
  echo "$connectionString"
  #echo "Endpoint=https://${signalrHostName};AccessKey=${accessKey};Version=1.0"
}

function delete_signalr_service()
{
  local name=$1
  local rsg=$2
  az signalr delete --name $name -g $rsg
}
