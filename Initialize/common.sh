#!/bin/bash

function init_common() {
    echo "init naming conventions and configs"
    PREFIX_PERF="${PREFIX}perfv2"
    RESOURCE_GROUP="${PREFIX_PERF}rg"
    STORAGE_ACCOUNT="${PREFIX_PERF}sa"
    KEYVAULT="${PREFIX_PERF}kv"
    KUBERNETES_SEVICES="${PREFIX_PERF}aks"
    PORTAL_IP_NAME="${PREFIX_PERF}ip"
    KV_SA_ACCESS_KEY="sa-accessKey"
    KV_KUBE_CONFIG="kube-config"
    SA_SHARE="perf"
    PORTAL_DNS="${PREFIX_PERF}-portal"
    WORK_SPACE="${PREFIX_PERF}la"
    SERVICE_PRINCIPAL="${PREFIX_PERF}sp"
    KVURL="https://${KEYVAULT}.vault.azure.net/"

    if [[ ! -z $CLOUD ]]; then
        az cloud set -n $CLOUD
    fi

    if [[ ! -z $SUBSCTIPTION ]]; then
        az account set -s $SUBSCTIPTION
    else
        SUBSCTIPTION=$(az account show --query "id" -o tsv)
    fi
}

function init_aks_group() {
    echo "init aks configs"
    AKS_RESOURCE_GROUP=$(az aks show -g $RESOURCE_GROUP -n $KUBERNETES_SEVICES --query nodeResourceGroup -o tsv)
    az aks get-credentials -g $RESOURCE_GROUP -n $KUBERNETES_SEVICES -a  --overwrite-existing
}

function throw_if_empty() {
    local name="$1"
    local value="$2"
    if [[ -z "$value" ]]; then
        echo "ERROR: Parameter $name cannot be empty." 1>&2
        exit -1
    fi
}

function replace() {
    local placeholder=$1
    local content=$2
    content=$(echo "${content////\\/}")
    sed -e "s/$placeholder/$content/g"
}
