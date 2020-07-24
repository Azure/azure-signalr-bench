#!/bin/bash

function init_common {
    PREFIX="${PREFIX}perf"
    RESOURCE_GROUP="${PREFIX}rg"
    STORAGE_ACCOUNT="${PREFIX}sa"
    KEYVAULT="${PREFIX}kv"
    KUBERNETES_SEVICES="${PREFIX}aks"

    KV_SA_ACCESS_KEY="sa-accessKey"
    KV_KUBE_CONFIG="kube-config"
    SA_SHARE="perf"

    if [[ ! -z $CLOUD ]];then
       az cloud set -n $CLOUD
    fi

    if [[ ! -z $SUBSCTIPTION ]];then
        az account set -s $SUBSCTIPTION
    else 
        SUBSCTIPTION=$(az account show --query "id" -o tsv)
    fi
}

function throw_if_empty {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]];then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}




