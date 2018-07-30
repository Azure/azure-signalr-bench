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

  if [ "$grps" != "" ]
  then
    delete_group $resgrp
  fi
  az group create --name $resgrp --location $location
}

function delete_group() {
  local rsg=$1
  az group delete --name $rsg -y
}

function create_signalr_service()
{
  local rsg=$1
  local name=$2
  local sku=$3
  local unitCount=$4
  local signalrHostName
  # add extension
  add_signalr_extension

  signalrHostName=$(az signalr create \
     --name $name                     \
     --resource-group $rsg            \
     --sku $sku                       \
     --unit-count $unitCount          \
     --query hostName                 \
     -o tsv)
  echo "$signalrHostName"
}

function check_signalr_service_dns()
{
  local rsg=$1
  local name=$2
  local nslookupData
  local externalIp=`az signalr show -n $name -g $rsg -o=json|jq ".externalIp"|tr -d '"'`
  #local hostname=`az signalr list --query [*].hostName --output table|grep "$name"`
  local hostname=${name}.servicedev.signalr.net
  local end=$((SECONDS + 120))
  while [ $SECONDS -lt $end ]
  do
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
  #local signalrHostName=`az signalr list --query [*].hostName --output table|grep "$signarl_service_name"`
  #if [ "$signalrHostName" == "" ]
  #then
  #   echo ""
  #   return
  #fi
  local signalrHostName=${signarl_service_name}.servicedev.signalr.net
  local accessKey=`az signalr key list --name $signarl_service_name --resource-group $rsg --query primaryKey -o tsv`
  echo "Endpoint=https://${signalrHostName};AccessKey=${accessKey}"
}

function delete_signalr_service()
{
  local name=$1
  local rsg=$2
  az signalr delete --name $name -g $rsg
}
