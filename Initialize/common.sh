#!/bin/bash

function init_common {
    PREFIX="${PREFIX0}perf"
    RESOURCE_GROUP="${PREFIX}rg"
    STORAGE_ACCOUNT="${PREFIX}sa"
    KEYVAULT="${PREFIX}kv"
    KUBERNETES_SEVICES="${PREFIX}aks"

    KV_SA_ACCESS_KEY="sa-accessKey"
    KV_KUBE_CONFIG="kube-config"

    if [[ ! -z $CLOUD ]];then
       az cloud set -n $CLOUD
    fi

    if [[ ! -z $SUBSCTIPTION ]];then
        az account set -s $SUBSCTIPTION
    else 
        SUBSCTIPTION=$(az account show --query "id" -o tsv)
    fi
}

function init_aks_group {
    AKS_RESOUCE_GROUP=$(az aks show -g $RESOURCE_GROUP -n $KUBERNETES_SEVICES --query nodeResourceGroup -o tsv)
    AKS_STORAGE_ACCOUNT="${PREFIX}akssa"
    SA_SHARE="perf"

}

function throw_if_empty {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]];then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}

function replace() {
    local placeholder=$1
    local content=$2
    content=$(echo "${content////\\/}" )
    sed -e "s/$placeholder/$content/g" 
}



